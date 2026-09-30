using System;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using Demo3D.Utilities;
using System.Drawing;
using System.Reflection;

// Created by Mark Emeott
// Updated June 30, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class RectangularLight : NativeObject {
        public RectangularLight(Visual sender) : base(sender) { Setup(sender); }

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - Light"), Description("On color")]
        public SimplePropertyValue<Color> OnColor;
        [Auto, Category("-Configuration - Light"), Description("Off color")]
        public SimplePropertyValue<Color> OffColor;
        [Auto, Category("-Configuration - Light"), Description("Calculate off color")]
        public SimplePropertyValue<Boolean> CalculateOffColor;
        [Auto, Category("-Configuration - Nametag"), Description("Show nametag"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ShowNametag;
        [Auto, Category("-Configuration - Connector"), Description("Connector enabled and visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorEnabled;
        [Auto, Category("-Configuration - Nametag"), Description("Nametag horizontal offset")]
        public SimplePropertyValue<DistanceProperty> NametagHorizontalOffset;
        [Auto, Category("-Configuration - Nametag"), Description("Nametag vertical offset")]
        public SimplePropertyValue<DistanceProperty> NametagVerticalOffset;
        [Auto, Category("-Wiring In"), Description("Light power")]
        public SimplePropertyValue<Boolean> R_LightPower;
        #endregion

        #region Events
        [Auto] void OnVisualAdded(Visual sender) {
            if (sender.Parent is SceneVisual) { Utilities.SnapToPanel(sender); }
        }

        [Auto] protected void OnReset(Visual sender) { Setup(sender); }

        private void Setup(Visual sender) {
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // update light
            UpdateLight(sender);
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

        [Auto] protected void OnR_LightPowerUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateLight(sender); }

        private void UpdateLight(Visual visual) {
            // update light color
            BoxVisual sender = visual as BoxVisual;
            sender.Material.Color = (R_LightPower.Value) ? OnColor.Value : OffColor.Value;
        }

        [Auto] protected void OnAfterPropertyUpdated(Visual sender, String name) {
            if (name == "Length" || name.Contains("Nametag") || name == "Depth" || name == "Height" || name == "Width") {
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

        [Auto] protected void OnOnColorUpdated(Visual sender, Color value, Color oldValue) { UpdateColors(sender); }
        [Auto] protected void OnOffColorUpdated(Visual sender, Color value, Color oldValue) { UpdateColors(sender); }
        [Auto] protected void OnCalculateOffColorUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateColors(sender); }

        private void UpdateColors(Visual sender) {
            // calculate off color
            if (CalculateOffColor.Value) {
                double hue; double saturation; double value;
                Utilities.ColorToHSV(OnColor.Value, out hue, out saturation, out value);
                OffColor.Value = Utilities.ColorFromHSV(hue, saturation, value * 100 / 255);
            }
            // update light
            UpdateLight(sender);
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