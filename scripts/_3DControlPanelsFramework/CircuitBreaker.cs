using System;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using System.Drawing;
using System.Reflection;
using Demo3D.Utilities;
using Demo3D.WiringDiagram;

// Created by Mark Emeott
// Updated June 30, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class CircuitBreaker : NativeObject {
        public CircuitBreaker(Visual sender) : base(sender) { Setup(sender); }

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - Contacts"), Description("Number of normally open contacts"), DefaultValue(1)]
        public SimplePropertyValue<Byte> NOContacts;
        [Auto, Category("-Configuration - Contacts"), Description("Number of normally closed contacts"), DefaultValue(1)]
        public SimplePropertyValue<Byte> NCContacts;
        [Auto, Category("-Configuration - Nametag"), Description("Show nametag"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ShowNametag;
        [Auto, Category("-Configuration - Connector"), Description("Connector enabled and visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorEnabled;
        [Auto, Category("-Configuration - Switch"), Description("Circuit breaker switch rotate speed"), DefaultValue(600.0)]
        public SimplePropertyValue<AngularSpeedProperty> RotateSpeed;
        [Auto, Category("-State - Circuit Breaker"), ReadOnly(true), Description("Circuit breaker on"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> CircuitBreakerOn;
        [Auto, Category("-State - Circuit Breaker"), ReadOnly(true), Description("Circuit breaker off"), DefaultValue(false)]
        public SimplePropertyValue<Boolean> CircuitBreakerOff;
        #endregion

        #region Events
        [Auto] void OnVisualAdded(Visual sender) {
            if (sender.Parent is SceneVisual) { Utilities.SnapToPanel(sender); }
        }

        [Auto] protected void OnReset(Visual sender) { Setup(sender); }

        private void Setup(Visual sender) {
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // calculate result
            CalculateResult(sender, null, null);
            // update connector
            UpdateConnector(sender);
            // setup contact listeners
            for (int i = 1; i <= NOContacts.Value; i++) {
                var cp = sender.GetCustomProperty("R_NO" + i);
                if (cp != null) {
                    cp.UpdatedScript.NativeListeners -= CalculateResult;
                    cp.UpdatedScript.NativeListeners += CalculateResult;
                }
            }
            for (int i = 1; i <= NCContacts.Value; i++) {
                var cp = sender.GetCustomProperty("R_NC" + i);
                if (cp != null) {
                    cp.UpdatedScript.NativeListeners -= CalculateResult;
                    cp.UpdatedScript.NativeListeners += CalculateResult;
                }
            }
        }

        [Auto] protected void OnShowNametagUpdated(Visual sender, Boolean value, Boolean oldValue) {
            var nametag = sender.FindChild("FrontPlate").FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Visible = ShowNametag.Value;
            }
        }

        [Auto] protected void OnClick(Visual sender, PickInfo info) {
            // toggle switch (in VR ActualVisualPicked is null)
            if (info.ActualVisualPicked == sender.FindChild("Switch") || info.ActualVisualPicked == null) {
                CircuitBreakerOn.Value = !CircuitBreakerOn.Value;
            }
        }

        [Auto] protected void OnCircuitBreakerOnUpdated(Visual sender, Boolean value, Boolean oldValue) {
            // update off value
            CircuitBreakerOff.Value = !CircuitBreakerOn.Value;
            // calculate result
            CalculateResult(sender, null, null);
            // calculate circuit breaker rotation
            var onRotation = -120.0;
            var offRotation = -60.0;
            var switchRotation = value ? onRotation : offRotation;
            var hinge = sender.FindImmediateChild("Hinge");
            // update text color
            TextVisual textOn = sender.FindChild("TextOn") as TextVisual;
            TextVisual textOff = sender.FindChild("TextOff") as TextVisual;
            textOn.Material.Color = (value) ? Color.Red : Color.Black;
            textOff.Material.Color = (value) ? Color.Black : Color.Green;
            if (app.Running) {
                // animate switch rotation
                hinge.RotateToLocalX(-switchRotation + 180, RotateSpeed.Value);
            }
            else {
                // warp switch to rotation immediately
                hinge.RotationDegrees = vector(switchRotation, -180, -90);
            }
            // refresh properties grid
            Utilities.RefreshPropertiesGrid(sender);
        }

        [Auto] protected void OnNOContactsUpdated(Visual sender, Byte value, Byte oldValue) {
            if (value > oldValue) {
                // add additional normally open contacts
                for (int i = oldValue + 1; i <= value; i++) {
                    if (!sender.HasCustomProperty("R_NO" + i)) {
                        var cp = sender.AddSimpleProperty("R_NO" + i, false, "Normally Open " + i + " Wiring In");
                        cp.Category = "-Wiring In";
                        cp.UpdatedScript.NativeListeners -= CalculateResult;
                        cp.UpdatedScript.NativeListeners += CalculateResult;
                        cp.PlaceOnLeftRail();
                    }
                    if (!sender.HasCustomProperty("W_NO" + i)) {
                        var cp = sender.AddSimpleProperty("W_NO" + i, false, "Normally Open " + i + " Wiring Out");
                        cp.Category = "-Wiring Out";
                        cp.ReadOnly = true;
                        cp.PlaceOnRightRail();
                    }
                }
            }
            else if (value < oldValue) {
                // remove extra normally open contacts
                for (int i = value + 1; i <= oldValue; i++) {
                    if (sender.HasCustomProperty("R_NO" + i)) {
                        var cp = sender.GetCustomProperty("R_NO" + i);
                        cp.RemoveFromWiringDiagram();
                        sender.RemoveCustomProperty(cp.Name);
                    }
                    if (sender.HasCustomProperty("W_NO" + i)) {
                        var cp = sender.GetCustomProperty("W_NO" + i);
                        cp.RemoveFromWiringDiagram();
                        sender.RemoveCustomProperty(cp.Name);
                    }
                }
            }
            // calculate result
            CalculateResult(sender, null, null);
            sender.AutoSizeHeightInWiringDiagram();
            sender.CompressPropertiesOnRails();
            // refresh wiring diagram & properties grid
            WiringActions.RefreshWiringDiagram(document);
            Utilities.RefreshPropertiesGrid(sender);
        }

        [Auto] protected void OnNCContactsUpdated(Visual sender, Byte value, Byte oldValue) {
            if (value > oldValue) {
                // add additional normally closed contacts
                for (int i = oldValue + 1; i <= value; i++) {
                    if (!sender.HasCustomProperty("R_NC" + i)) {
                        var cp = sender.AddSimpleProperty("R_NC" + i, false, "Normally Closed " + i + " Wiring In");
                        cp.Category = "-Wiring In";
                        cp.UpdatedScript.NativeListeners -= CalculateResult;
                        cp.UpdatedScript.NativeListeners += CalculateResult;
                        cp.PlaceOnLeftRail();
                    }
                    if (!sender.HasCustomProperty("W_NC" + i)) {
                        var cp = sender.AddSimpleProperty("W_NC" + i, false, "Normally Closed " + i + " Wiring Out");
                        cp.Category = "-Wiring Out";
                        cp.ReadOnly = true;
                        cp.PlaceOnRightRail();
                    }
                }
            }
            else if (value < oldValue) {
                // remove extra normally closed contacts
                for (int i = value + 1; i <= oldValue; i++) {
                    if (sender.HasCustomProperty("R_NC" + i)) {
                        var cp = sender.GetCustomProperty("R_NC" + i);
                        cp.RemoveFromWiringDiagram();
                        sender.RemoveCustomProperty(cp.Name);
                    }
                    if (sender.HasCustomProperty("W_NC" + i)) {
                        var cp = sender.GetCustomProperty("W_NC" + i);
                        cp.RemoveFromWiringDiagram();
                        sender.RemoveCustomProperty(cp.Name);
                    }
                }
            }
            // calculate result
            CalculateResult(sender, null, null);
            sender.AutoSizeHeightInWiringDiagram();
            sender.CompressPropertiesOnRails();
            // refresh wiring diagram & properties grid
            WiringActions.RefreshWiringDiagram(document);
            Utilities.RefreshPropertiesGrid(sender);
        }

        private void CalculateResult(Visual sender, object _value, object _oldValue) {
            // calculate normally open contact output values
            for (int i = 1; i <= NOContacts.Value; i++) {
                if (sender.HasCustomProperty("R_NO" + i) && sender.HasCustomProperty("W_NO" + i)) {
                    sender.SetCustomPropertyValue("W_NO" + i, (Boolean)sender.GetCustomPropertyValue("R_NO" + i) && CircuitBreakerOn.Value);
                }
            }
            // calculate normally closed contact output values
            for (int i = 1; i <= NCContacts.Value; i++) {
                if (sender.HasCustomProperty("R_NC" + i) && sender.HasCustomProperty("W_NC" + i)) {
                    sender.SetCustomPropertyValue("W_NC" + i, (Boolean)sender.GetCustomPropertyValue("R_NC" + i) && !CircuitBreakerOn.Value);
                }
            }
        }

        [Auto] protected void OnAfterPropertyUpdated(Visual sender, String name) {
            if (name == "Depth" || name == "Height" || name == "Width") {
                // update connector
                UpdateConnector(sender);
            }
        }

        [Auto] protected void OnConnectorEnabledUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnector(sender); }
        [Auto] protected void OnParentUpdated(Visual sender, Visual oldParent, Visual newParent) { UpdateConnector(sender); }
        #endregion

        #region Connector Configuration
        private void UpdateConnector(Visual visual) {
            // create panel connector on back
            BoxVisual sender = visual as BoxVisual;
            Demo3D.Visuals.Connector c = sender.FindCreateConnector("C1");
            c.Start = vector(0.0, 0.0, sender.Depth / 2);
            c.End = vector(-0.00001, 0.0, sender.Depth / 2);
            c.Normal = vector(0, 0, 1);
            c.Type = "PanelComponent";
            c.Allowed = new string[] { "Panel" };
            c.ReparentOnConnect = true;
            c.AlignmentStyle = ConnectorAlignmentStyle.Complete;
            c.MaxAllowedConnections = 1;
            c.AutoConnect = (ConnectorEnabled.Value) ? true : false;
            c.ControlPointEnabled = (ConnectorEnabled.Value) ? true : false;
            c.ControlPointSize = Math.Min(sender.Height, sender.Width) / 10;
            c.TextHeight = Math.Min(sender.Height, sender.Width) / 10;
            // set snap distance
            var xSpacing = sender.Width;
            var ySpacing = sender.Height;
            foreach (var ancestor in sender.Ancestors) {
                if (ancestor.Type.StartsWith("Panel") && ancestor.HasCustomProperty("XSpacing") && ancestor.HasCustomProperty("YSpacing")) {
                    xSpacing = ancestor.GetCustomPropertyValue("XSpacing") as DistanceProperty;
                    ySpacing = ancestor.GetCustomPropertyValue("YSpacing") as DistanceProperty;
                }
            }
            c.SnapDistance = Math.Min(xSpacing, ySpacing);
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