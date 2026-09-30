using System;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using Demo3D.Utilities;
using System.Drawing;
using System.Reflection;
using Demo3D.PLC.Comms;

// Created by Mark Emeott
// Updated June 30, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class RectangularLED : NativeObject {
        public RectangularLED(Visual sender) : base(sender) { Setup(sender); }

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - LED"), Description("LED colors array")]
        public SimplePropertyValue<DataArray<Color>> LEDColors;
        [Auto, Category("-Configuration - LED"), Description("Unpowered color")]
        public SimplePropertyValue<Color> UnpoweredColor;
        [Auto, Category("-Configuration - LED"), Description("Calculate unpowered color automatically"), DefaultValue(false)]
        public SimplePropertyValue<Boolean> CalculateUnpoweredColor;
        [Auto, Category("-Configuration - Nametag"), Description("Show nametag"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ShowNametag;
        [Auto, Category("-Configuration - Connector"), Description("Connector enabled and visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorEnabled;
        [Auto, Category("-Configuration - Nametag"), Description("Nametag horizontal offset")]
        public SimplePropertyValue<DistanceProperty> NametagHorizontalOffset;
        [Auto, Category("-Configuration - Nametag"), Description("Nametag vertical offset")]
        public SimplePropertyValue<DistanceProperty> NametagVerticalOffset;
        [Auto, Category("-Wiring In"), Description("LED power")]
        public SimplePropertyValue<Boolean> R_LEDPower;
        [Auto, Category("-Wiring In"), Description("LED color index number (lookup in LEDColors array)")]
        public SimplePropertyValue<Int32> R_LEDColor;
        #endregion

        #region Events
        [Auto] void OnVisualAdded(Visual sender) {
            if (sender.Parent is SceneVisual) { Utilities.SnapToPanel(sender); }
        }

        [Auto] protected void OnReset(Visual sender) { Setup(sender); }

        private void Setup(Visual sender) {
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // update led
            UpdateLED(sender);
            // update nametag location
            UpdateNametagLocation(sender);
            // update connector
            UpdateConnector(sender);
        }

        [Auto] protected void OnShowNametagUpdated(Visual sender, Boolean value, Boolean oldValue) {
            var nametag = sender.FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Visible = ShowNametag.Value;
            }
        }

        [Auto] protected void OnR_LEDPowerUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateLED(sender); }

        private void UpdateLED(Visual visual) {
            // update led color
            BoxVisual sender = visual as BoxVisual;
            try {
                if (!R_LEDPower.Value || R_LEDColor.Value < 0 || R_LEDColor.Value >= LEDColors.Value.Count) {
                    sender.Material.Color = UnpoweredColor.Value;
                }
                else {
                    sender.Material.Color = LEDColors.Value[R_LEDColor.Value];
                }
            }
            catch (Exception) {
                sender.Material.Color = UnpoweredColor.Value;
            }
        }

        [Auto] protected void OnAfterPropertyUpdated(Visual sender, String name) {
            if (name == "Length" || name.Contains("Nametag") || name == "Diameter" || name == "Radius") {
                // update nametag location
                UpdateNametagLocation(sender);
                // update connector
                UpdateConnector(sender);
            }
        }

        private void UpdateNametagLocation(Visual visual) {
            //update nametag location
            BoxVisual sender = visual as BoxVisual;
            TextVisual nametag = sender.FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Location = vector(NametagHorizontalOffset.Value, NametagVerticalOffset.Value, -sender.Depth / 2 - nametag.Depth / 2 - 0.0002);
            }
        }

        [Auto] protected void OnR_LEDColorUpdated(Visual sender, Int32 value, Int32 oldValue) { UpdateUnpoweredColor(sender); }
        [Auto] protected void OnLEDColorsUpdated(Visual sender, DataArray<Color> value, DataArray<Color> oldValue) { UpdateUnpoweredColor(sender); }
        [Auto] protected void OnUnpoweredColorUpdated(Visual sender, Color value, Color oldValue) { UpdateUnpoweredColor(sender); }
        [Auto] protected void OnCalculateUnpoweredColorUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateUnpoweredColor(sender); }

        private void UpdateUnpoweredColor(Visual sender) {
            try {
                // calculate unpowered color (if enabled)
                if (CalculateUnpoweredColor.Value) {
                    double hue; double saturation; double value;
                    Utilities.ColorToHSV(LEDColors.Value[R_LEDColor.Value], out hue, out saturation, out value);
                    UnpoweredColor.Value = Utilities.ColorFromHSV(hue, saturation, value * 100 / 255);
                }
            }
            catch (Exception) { }
            // update led
            UpdateLED(sender);
            // refresh properties grid
            Utilities.RefreshPropertiesGrid(sender);
        }

        [Auto] protected void OnConnectorEnabledUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnector(sender); }
        [Auto] protected void OnParentUpdated(Visual sender, Visual oldParent, Visual newParent) { UpdateConnector(sender); }
        #endregion

        #region Connector Configuration
        private void UpdateConnector(Visual visual) {
            // create panel connector on back
            BoxVisual sender = visual as BoxVisual;
            Connector c = sender.FindCreateConnector("C1");
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