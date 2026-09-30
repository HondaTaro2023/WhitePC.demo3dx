using System;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using Demo3D.Utilities;
using System.Reflection;
using System.Linq;

// Created by Mark Emeott
// Updated September 15, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class Panel : NativeObject {
        public Panel(Visual sender) : base(sender) { Setup(sender); }

        #region Visuals
        [Auto] ContainerVisual Door;
        [Auto] CylinderVisual Hinge;
        #endregion

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - Connectors"), Description("Specified connectors are enabled & visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorsEnabled;
        [Auto, Category("-Configuration - Connectors"), Description("Enable door front connectors"), DefaultValue(true)]
        public CustomPropertyValue<Boolean> DoorFront;
        [Auto, Category("-Configuration - Connectors"), Description("Enable door back connectors"), DefaultValue(true)]
        public CustomPropertyValue<Boolean> DoorBack;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel inside back connectors"), DefaultValue(true)]
        public CustomPropertyValue<Boolean> InsideBack;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel inside left connectors"), DefaultValue(false)]
        public CustomPropertyValue<Boolean> InsideLeft;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel inside right connectors"), DefaultValue(false)]
        public CustomPropertyValue<Boolean> InsideRight;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel inside top connectors"), DefaultValue(false)]
        public CustomPropertyValue<Boolean> InsideTop;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel inside bottom connectors"), DefaultValue(false)]
        public CustomPropertyValue<Boolean> InsideBottom;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel outside left connectors"), DefaultValue(true)]
        public CustomPropertyValue<Boolean> OutsideLeft;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel outside right connectors"), DefaultValue(true)]
        public CustomPropertyValue<Boolean> OutsideRight;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel outside top connectors"), DefaultValue(true)]
        public CustomPropertyValue<Boolean> OutsideTop;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel outside bottom connectors"), DefaultValue(false)]
        public CustomPropertyValue<Boolean> OutsideBottom;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel outside back connectors"), DefaultValue(false)]
        public CustomPropertyValue<Boolean> OutsideBack;
        [Auto, Category("-Configuration - Connectors"), Description("X spacing for connectors"), DefaultValue(0.05)]
        public SimplePropertyValue<DistanceProperty> XSpacing;
        [Auto, Category("-Configuration - Connectors"), Description("X border at left and right edges"), DefaultValue(0.05)]
        public SimplePropertyValue<DistanceProperty> XBorder;
        [Auto, Category("-Configuration - Connectors"), Description("Y spacing for connectors"), DefaultValue(0.05)]
        public SimplePropertyValue<DistanceProperty> YSpacing;
        [Auto, Category("-Configuration - Connectors"), Description("Y border at top and bottom edges"), DefaultValue(0.05)]
        public SimplePropertyValue<DistanceProperty> YBorder;
        [Auto, Category("-Configuration - Door"), Description("Door thickness"), DefaultValue(0.02)]
        public SimplePropertyValue<DistanceProperty> DoorThickness;
        public enum DoorHingeSides { Left, Right }
        [Auto, Category("-Configuration - Door"), Description("Door hinge side (Left or Right)"), DefaultValue(DoorHingeSides.Left)]
        public SimplePropertyValue<DoorHingeSides> DoorHingeSide;
        [Auto, Category("-Configuration - Door"), Description("Door opened rotation"), DefaultValue(170.0)]
        public CustomPropertyValue<AngleProperty> DoorOpenedRotation;
        [Auto, Category("-Configuration - Door"), ReadOnly(true), Description("Door opened")]
        public CustomPropertyValue<Boolean> DoorOpened;
        [Auto, Category("-Configuration - Nametag"), Description("Show nametag"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ShowNametag;
        [Auto, Category("-Constants"), ReadOnly(true), Hidden(true), DefaultValue(true), Description("Always on")]
        public SimplePropertyValue<Boolean> AlwaysOn;
        [Auto, Category("-Constants"), ReadOnly(true), Hidden(true), DefaultValue(false), Description("Always off")]
        public SimplePropertyValue<Boolean> AlwaysOff;
        #endregion

        #region Events
        [Auto] protected void OnReset(Visual sender) { Setup(sender); }

        private void Setup(Visual sender) {
            // update visual references
            Hinge = sender.Children.FirstOrDefault(v => v.Type == "Hinge") as CylinderVisual;
            if (Hinge == null) {
                app.LogMessage("Exception", "Panel is missing a Hinge child visual", sender);
                return;
            }
            Door = Hinge.Children.FirstOrDefault(v => v.Type == "Door") as ContainerVisual;
            if (Door == null) {
                app.LogMessage("Exception", "Panel is missing a Door child visual", sender);
                return;
            }
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // update connectors
            UpdateConnectors(sender);
            // update listeners
            Hinge.OnMoveToCompleted.NativeListeners -= OnMoveToCompleted_NativeListeners;
            Hinge.OnMoveToCompleted.NativeListeners += OnMoveToCompleted_NativeListeners;
        }

        [Auto] protected void OnShowNametagUpdated(Visual sender, Boolean value, Boolean oldValue) {
            var nametag = Door?.FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Visible = ShowNametag.Value;
            }
        }

        [Auto] protected void OnClick(Visual sender, PickInfo info) {
            // respond to door click (in VR ActualVisualPicked is null)
            if (info.ActualVisualPicked == Door || info.ActualVisualPicked == null) {
                var locks = sender.FindVisualType("Lock");
                if (locks.Count() > 0) {
                    // door has lock
                    var locked = locks.First().GetCustomPropertyValue<Boolean>("Locked", true);
                    if (!locked) {
                        // door unlocked, move door
                        MoveDoor(sender);
                    }
                }
                else {
                    // door has no lock, move door
                    MoveDoor(sender);
                }
            }
        }

        private void MoveDoor(Visual sender) {
            var sideMultiplier = DoorHingeSide.Value == DoorHingeSides.Left ? 1 : -1;
            var degrees = Hinge.RotationYDegrees == 0 ? DoorOpenedRotation.Value * sideMultiplier : 0;
            Hinge.CancelAnimators();
            if (app.Running) {
                // animate door opening/closing
                Hinge.RotateToLocalY(degrees, 180);
            }
            else {
                // warp door to opened/closed immediately
                Hinge.RotationYDegrees = degrees;
                // update door opened
                UpdateDoorOpened();
            }
        }

        private void OnMoveToCompleted_NativeListeners(Visual sender) {
            // update door opened
            UpdateDoorOpened();
        }

        private void UpdateDoorOpened() {
            // hinge rotation is negative for right hinged doors, so compare magnitudes
            DoorOpened.Value = Demo3D.Common.Util.EQ(Math.Abs(Hinge.RotationYDegrees), Math.Abs(DoorOpenedRotation.Value), 0.001);
        }

        [Auto] protected void OnAfterPropertyUpdated(Visual sender, String name) {
            if (name == "Depth" || name == "Height" || name == "Width" || name == "Thickness") {
                // update door & connectors
                UpdateDoor(sender);
            }
        }

        [Auto] protected void OnDoorHingeSideUpdated(Visual sender, DoorHingeSides value, DoorHingeSides oldValue) { UpdateDoor(sender); }
        [Auto] protected void OnDoorThicknessUpdated(Visual sender, DistanceProperty value, DistanceProperty oldValue) { UpdateDoor(sender); }

        private void UpdateDoor(Visual visual) {
            ContainerVisual sender = visual as ContainerVisual;
            if (Hinge == null || Door == null) { return; }
            // update hinge size & position
            var sideMultiplier = DoorHingeSide.Value == DoorHingeSides.Left ? 1 : -1;
            Hinge.Length = sender.Height * 0.995;
            Hinge.Radius = DoorThickness.Value / 5;
            Hinge.Location = vector(-sender.Width / 2 * sideMultiplier, 0, -sender.Depth / 2);
            Hinge.RotationDegrees = vector(0, Math.Abs(Hinge.RotationYDegrees) * sideMultiplier, 0);
            // update door size & position
            Door.Height = sender.Height;
            Door.Width = sender.Width;
            Door.Thickness = DoorThickness.Value;
            Door.Depth = DoorThickness.Value;
            Door.Location = vector(sender.Width / 2 * sideMultiplier, 0, -DoorThickness.Value / 2);
            Door.RotationDegrees = vector(0, 0, 0);
            // update lock position if connected to front door connector
            var locks = Door.FindVisualType("Lock").Where((l) => l.Parent == Door);
            foreach (var l in locks) {
                var lockVisual = l as BoxTubeVisual;
                if (lockVisual == null) { continue; }
                var connector = lockVisual.FindConnector("C1");
                if (connector?.ConnectedTo == null) { continue; }
                if (connector.ConnectedTo.Name.StartsWith("DoorFront")) {
                    lockVisual.LocationZ = -(Door.Thickness / 2 + lockVisual.Height / 2);
                }
            }
            // update connectors
            UpdateConnectors(sender);
        }

        [Auto] protected void OnXSpacingUpdated(Visual sender, DistanceProperty value, DistanceProperty oldValue) {
            // error check XSpacing value
            if (XSpacing.Value <= 0) {
                app.LogMessage("Exception", "XSpacing must be greater than zero", sender);
                XSpacing.Value = oldValue;
                return;
            }
            // update connectors
            UpdateConnectors(sender);
        }

        [Auto] protected void OnYSpacingUpdated(Visual sender, DistanceProperty value, DistanceProperty oldValue) {
            // error check YSpacing value
            if (YSpacing.Value <= 0) {
                app.LogMessage("Exception", "YSpacing must be greater than zero", sender);
                YSpacing.Value = oldValue;
                return;
            }
            // update connectors
            UpdateConnectors(sender);
        }

        [Auto] protected void OnXBorderUpdated(Visual sender, DistanceProperty value, DistanceProperty oldValue) {
            // error check XBorder value
            if (XBorder.Value < 0) {
                app.LogMessage("Exception", "XBorder must be greater than or equal to zero", sender);
                XBorder.Value = oldValue;
                return;
            }
            // update connectors
            UpdateConnectors(sender);
        }

        [Auto] protected void OnYBorderUpdated(Visual sender, DistanceProperty value, DistanceProperty oldValue) {
            // error check YBorder value
            if (YBorder.Value < 0) {
                app.LogMessage("Exception", "YBorder must be greater than or equal to zero", sender);
                YBorder.Value = oldValue;
                return;
            }
            // update connectors
            UpdateConnectors(sender);
        }

        [Auto] protected void OnConnectorsEnabledUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnDoorFrontUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnDoorBackUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnInsideBackUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnInsideLeftUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnInsideRightUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnInsideTopUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnInsideBottomUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnOutsideLeftUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnOutsideRightUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnOutsideTopUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnOutsideBottomUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnOutsideBackUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnDoorOpenedUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }

        private void UpdateConnectors(Visual visual) {
            // calculate number of connector rows to create
            ContainerVisual sender = visual as ContainerVisual;
            if (Door == null) { return; }
            if (XSpacing.Value <= 0 || YSpacing.Value <= 0) {
                app.LogMessage("Exception", "XSpacing and YSpacing must be greater than zero", visual);
                return;
            }
            int numRows = ((int)(sender.Height / YSpacing.Value + 0.001) < 1) ? 1 : (int)((sender.Height - YBorder.Value * 2) / YSpacing.Value + 1.001);
            int numColumns = ((int)(sender.Width / XSpacing.Value + 0.001) < 1) ? 1 : (int)((sender.Width - XBorder.Value * 2) / XSpacing.Value + 1.001);
            // create connectors on faces
            CreatePanelConnectors("DoorFront", DoorFront.Value ? numRows : 0, DoorFront.Value ? numColumns : 0);
            CreatePanelConnectors("DoorBack", DoorBack.Value ? numRows : 0, DoorBack.Value ? numColumns : 0);
            CreatePanelConnectors("InsideBack", InsideBack.Value ? numRows : 0, InsideBack.Value ? numColumns : 0);
            CreatePanelConnectors("InsideLeft", InsideLeft.Value ? numRows : 0, InsideLeft.Value ? 1 : 0);
            CreatePanelConnectors("InsideRight", InsideRight.Value ? numRows : 0, InsideRight.Value ? 1 : 0);
            CreatePanelConnectors("InsideTop", InsideTop.Value ? 1 : 0, InsideTop.Value ? numColumns : 0);
            CreatePanelConnectors("InsideBottom", InsideBottom.Value ? 1 : 0, InsideBottom.Value ? numColumns : 0);
            CreatePanelConnectors("OutsideLeft", OutsideLeft.Value ? numRows : 0, OutsideLeft.Value ? 1 : 0);
            CreatePanelConnectors("OutsideRight", OutsideRight.Value ? numRows : 0, OutsideRight.Value ? 1 : 0);
            CreatePanelConnectors("OutsideTop", OutsideTop.Value ? 1 : 0, OutsideTop.Value ? numColumns : 0);
            CreatePanelConnectors("OutsideBottom", OutsideBottom.Value ? 1 : 0, OutsideBottom.Value ? numColumns : 0);
            CreatePanelConnectors("OutsideBack", OutsideBack.Value ? numRows : 0, OutsideBack.Value ? numColumns : 0);
            // remove deprecated connector
            sender.RemoveConnector("C1");
        }
        #endregion

        #region Connector Configuration
        private void CreatePanelConnectors(string prefix, Int32 numRows, Int32 numColumns) {
            // create door & panel connectors
            ContainerVisual sender = Visual as ContainerVisual;
            if (Door == null) { return; }
            var xZero = (numColumns == 1) ? 0.0 : -sender.Width / 2 + XBorder.Value;
            var yZero = (numRows == 1) ? 0.0 : sender.Height / 2 - YBorder.Value;
            var n = 0;
            for (int r = 1; r <= numRows; r++) {
                for (int c = 1; c <= numColumns; c++) {
                    n++;
                    if (prefix.Contains("Door")) {
                        // door connector
                        Demo3D.Visuals.Connector connector = Door.FindCreateConnector(prefix + n);
                        if (prefix == "DoorFront") {
                            var xLoc = xZero + XSpacing.Value * (c - 1);
                            var yLoc = yZero - YSpacing.Value * (r - 1);
                            connector.Start = vector(xLoc, yLoc, -Door.Depth / 2);
                            connector.End = vector(xLoc + 0.00001, yLoc, -Door.Depth / 2);
                            connector.Normal = vector(0, 0, -1);
                        }
                        else if (prefix == "DoorBack") {
                            var xLoc = -(xZero + XSpacing.Value * (c - 1));
                            var yLoc = yZero - YSpacing.Value * (r - 1);
                            connector.Start = vector(xLoc, yLoc, Door.Depth / 2);
                            connector.End = vector(xLoc - 0.00001, yLoc, Door.Depth / 2);
                            connector.Normal = vector(0, 0, 1);
                        }
                        SetConnectorProperties(connector);
                    }
                    else {
                        // panel connector
                        Demo3D.Visuals.Connector connector = sender.FindCreateConnector(prefix + n);
                        if (prefix == "InsideBack") {
                            var xLoc = xZero + XSpacing.Value * (c - 1);
                            var yLoc = yZero - YSpacing.Value * (r - 1);
                            connector.Start = vector(xLoc, yLoc, sender.Depth / 2 - sender.Thickness);
                            connector.End = vector(xLoc + 0.00001, yLoc, sender.Depth / 2 - sender.Thickness);
                            connector.Normal = vector(0, 0, -1);
                        }
                        else if (prefix == "InsideLeft") {
                            var yLoc = yZero - YSpacing.Value * (r - 1);
                            connector.Start = vector(-sender.Width / 2 + sender.Thickness, yLoc, -sender.Thickness / 2);
                            connector.End = vector(-sender.Width / 2 + sender.Thickness, yLoc, -sender.Thickness / 2 + 0.00001);
                            connector.Normal = vector(1, 0, 0);
                        }
                        else if (prefix == "InsideRight") {
                            var yLoc = yZero - YSpacing.Value * (r - 1);
                            connector.Start = vector(sender.Width / 2 - sender.Thickness, yLoc, -sender.Thickness / 2);
                            connector.End = vector(sender.Width / 2 - sender.Thickness, yLoc, -sender.Thickness / 2 - 0.00001);
                            connector.Normal = vector(-1, 0, 0);
                        }
                        else if (prefix == "InsideTop") {
                            var xLoc = xZero + XSpacing.Value * (c - 1);
                            var yLoc = sender.Height / 2;
                            connector.Start = vector(xLoc, yLoc - sender.Thickness, -sender.Thickness / 2);
                            connector.End = vector(xLoc + 0.00001, yLoc - sender.Thickness, -sender.Thickness / 2);
                            connector.Normal = vector(0, -1, 0);
                        }
                        else if (prefix == "InsideBottom") {
                            var xLoc = xZero + XSpacing.Value * (c - 1);
                            var yLoc = -sender.Height / 2;
                            connector.Start = vector(xLoc, yLoc + sender.Thickness, -sender.Thickness / 2);
                            connector.End = vector(xLoc + 0.00001, yLoc + sender.Thickness, -sender.Thickness / 2);
                            connector.Normal = vector(0, 1, 0);
                        }
                        else if (prefix == "OutsideLeft") {
                            var yLoc = yZero - YSpacing.Value * (r - 1);
                            connector.Start = vector(-sender.Width / 2, yLoc, 0);
                            connector.End = vector(-sender.Width / 2, yLoc, -0.00001);
                            connector.Normal = vector(-1, 0, 0);
                        }
                        else if (prefix == "OutsideRight") {
                            var yLoc = yZero - YSpacing.Value * (r - 1);
                            connector.Start = vector(sender.Width / 2, yLoc, 0);
                            connector.End = vector(sender.Width / 2, yLoc, 0.00001);
                            connector.Normal = vector(1, 0, 0);
                        }
                        else if (prefix == "OutsideTop") {
                            var xLoc = xZero + XSpacing.Value * (c - 1);
                            var yLoc = sender.Height / 2;
                            connector.Start = vector(xLoc, yLoc, 0);
                            connector.End = vector(xLoc + 0.00001, yLoc, 0);
                            connector.Normal = vector(0, 1, 0);
                        }
                        else if (prefix == "OutsideBottom") {
                            var xLoc = xZero + XSpacing.Value * (c - 1);
                            var yLoc = -sender.Height / 2;
                            connector.Start = vector(xLoc, yLoc, 0);
                            connector.End = vector(xLoc + 0.00001, yLoc, 0);
                            connector.Normal = vector(0, -1, 0);
                        }
                        else if (prefix == "OutsideBack") {
                            var xLoc = -(xZero + XSpacing.Value * (c - 1));
                            var yLoc = yZero - YSpacing.Value * (r - 1);
                            connector.Start = vector(xLoc, yLoc, sender.Depth / 2);
                            connector.End = vector(xLoc - 0.00001, yLoc, sender.Depth / 2);
                            connector.Normal = vector(0, 0, 1);
                        }
                        SetConnectorProperties(connector);
                    }
                }
            }
            // remove extra panel connectors (snapshot the collection, it is modified while removing)
            foreach (var c in sender.AllConnectors.ToList()) {
                if (c.Type == "Panel" && c.Name.StartsWith(prefix)) {
                    Int32 connectorNumber;
                    if (Int32.TryParse(c.Name.Substring(prefix.Length), out connectorNumber) && connectorNumber > numRows * numColumns) {
                        sender.RemoveConnector(c.Name);
                    }
                }
            }
            // remove extra door connectors (snapshot the collection, it is modified while removing)
            foreach (var c in Door.AllConnectors.ToList()) {
                if (c.Type == "Panel" && c.Name.StartsWith(prefix)) {
                    Int32 connectorNumber;
                    if (Int32.TryParse(c.Name.Substring(prefix.Length), out connectorNumber) && connectorNumber > numRows * numColumns) {
                        Door.RemoveConnector(c.Name);
                    }
                }
            }
        }

        private void SetConnectorProperties(Demo3D.Visuals.Connector connector) {
            // set connector properties
            connector.Type = "Panel";
            connector.Allowed = new string[] { "PanelComponent" };
            connector.ReparentOnConnect = false;
            connector.AlignmentStyle = ConnectorAlignmentStyle.Complete;
            connector.MaxAllowedConnections = 1;
            var enableConnectors = false;
            if (ConnectorsEnabled.Value) {
                if (connector.Name.StartsWith("DoorBack") || connector.Name.StartsWith("Inside")) {
                    if (DoorOpened.Value) {
                        enableConnectors = true;
                    }
                }
                else {
                    enableConnectors = true;
                }
            }
            connector.AutoConnect = enableConnectors;
            connector.ControlPointEnabled = enableConnectors;
            connector.ControlPointSize = Math.Min(XSpacing.Value, YSpacing.Value) / 10;
            connector.TextHeight = Math.Min(XSpacing.Value, YSpacing.Value) / 10;
            connector.SnapDistance = Math.Max(XSpacing.Value, YSpacing.Value) / 2 + 0.001;
        }

        [Auto] protected void OnDragStart(Visual sender) {
            // disable show connectors while dragging (prevent red connector boxes showing)
            sender.UserVars["ShowConnectorsState"] = app.BuilderTool.ShowConnectors;
            app.BuilderTool.ShowConnectors = false;
        }

        [Auto] protected void OnDragEnd(Visual sender) {
            // restore show connectors state
            try { app.BuilderTool.ShowConnectors = (Boolean)sender.UserVars["ShowConnectorsState"]; }
            catch (Exception) { }
        }
        #endregion
    }
}