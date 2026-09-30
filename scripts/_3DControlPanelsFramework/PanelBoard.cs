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
    [Auto] public class PanelBoard : NativeObject {
        public PanelBoard(Visual sender) : base(sender) { Setup(sender); }

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - Connectors"), Description("Specified connectors are enabled & visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorsEnabled;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel board front connectors"), DefaultValue(true)]
        public CustomPropertyValue<Boolean> PanelBoardFront;
        [Auto, Category("-Configuration - Connectors"), Description("Enable panel board back connectors"), DefaultValue(false)]
        public CustomPropertyValue<Boolean> PanelBoardBack;
        [Auto, Category("-Configuration - Connectors"), Description("X spacing for connectors"), DefaultValue(0.05)]
        public SimplePropertyValue<DistanceProperty> XSpacing;
        [Auto, Category("-Configuration - Connectors"), Description("X border at left and right edges"), DefaultValue(0.05)]
        public SimplePropertyValue<DistanceProperty> XBorder;
        [Auto, Category("-Configuration - Connectors"), Description("Y spacing for connectors"), DefaultValue(0.05)]
        public SimplePropertyValue<DistanceProperty> YSpacing;
        [Auto, Category("-Configuration - Connectors"), Description("Y border at top and bottom edges"), DefaultValue(0.05)]
        public SimplePropertyValue<DistanceProperty> YBorder;
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
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // update connectors
            UpdateConnectors(sender);
        }

        [Auto] protected void OnAfterPropertyUpdated(Visual sender, String name) {
            if (name == "Depth" || name == "Height" || name == "Width" || name == "Thickness") {
                // update connectors
                UpdateConnectors(sender);
            }
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
        [Auto] protected void OnPanelBoardFrontUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }
        [Auto] protected void OnPanelBoardBackUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnectors(sender); }

        private void UpdateConnectors(Visual visual) {
            // calculate number of connector rows to create
            BoxVisual sender = visual as BoxVisual;
            int numRows = ((int)(sender.Height / YSpacing.Value + 0.001) < 1) ? 1 : (int)((sender.Height - YBorder.Value * 2) / YSpacing.Value + 1.001);
            int numColumns = ((int)(sender.Width / XSpacing.Value + 0.001) < 1) ? 1 : (int)((sender.Width - XBorder.Value * 2) / XSpacing.Value + 1.001);
            // create connectors on faces
            CreateBoardConnectors("PanelBoardFront", PanelBoardFront.Value ? numRows : 0, PanelBoardFront.Value ? numColumns : 0);
            CreateBoardConnectors("PanelBoardBack", PanelBoardBack.Value ? numRows : 0, PanelBoardBack.Value ? numColumns : 0);
            // remove deprecated connector
            sender.RemoveConnector("C1");
        }
        #endregion

        #region Connector Configuration
        private void CreateBoardConnectors(string prefix, Int32 numRows, Int32 numColumns) {
            // create panel connectors
            BoxVisual sender = Visual as BoxVisual;
            var xZero = (numColumns == 1) ? 0.0 : -sender.Width / 2 + XBorder.Value;
            var yZero = (numRows == 1) ? 0.0 : sender.Height / 2 - YBorder.Value;
            var n = 0;
            for (int r = 1; r <= numRows; r++) {
                for (int c = 1; c <= numColumns; c++) {
                    n++;
                    Demo3D.Visuals.Connector connector = sender.FindCreateConnector(prefix + n);
                    if (prefix == "PanelBoardFront") {
                        var xLoc = xZero + XSpacing.Value * (c - 1);
                        var yLoc = yZero - YSpacing.Value * (r - 1);
                        connector.Start = vector(xLoc, yLoc, -sender.Depth / 2);
                        connector.End = vector(xLoc + 0.00001, yLoc, -sender.Depth / 2);
                        connector.Normal = vector(0, 0, -1);
                    }
                    else if (prefix == "PanelBoardBack") {
                        var xLoc = -(xZero + XSpacing.Value * (c - 1));
                        var yLoc = yZero - YSpacing.Value * (r - 1);
                        connector.Start = vector(xLoc, yLoc, sender.Depth / 2);
                        connector.End = vector(xLoc - 0.00001, yLoc, sender.Depth / 2);
                        connector.Normal = vector(0, 0, 1);
                    }
                    SetConnectorProperties(connector);
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
        }

        private void SetConnectorProperties(Demo3D.Visuals.Connector connector) {
            // set connector properties
            connector.Type = "Panel";
            connector.Allowed = new string[] { "PanelComponent" };
            connector.ReparentOnConnect = false;
            connector.AlignmentStyle = ConnectorAlignmentStyle.Complete;
            connector.MaxAllowedConnections = 1;
            connector.AutoConnect = ConnectorsEnabled.Value;
            connector.ControlPointEnabled = ConnectorsEnabled.Value;
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