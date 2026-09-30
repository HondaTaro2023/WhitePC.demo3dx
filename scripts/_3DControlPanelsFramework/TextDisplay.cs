using System;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using Demo3D.Utilities;
using System.Reflection;

// Created by Mark Emeott
// Updated June 30, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class TextDisplay : NativeObject {
        public TextDisplay(Visual sender) : base(sender) { Setup(sender); }

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - Connector"), Description("Connector enabled and visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorEnabled;
        [Auto, Category("-Wiring In"), Description("Text display value"), DefaultValue("Text")]
        public SimplePropertyValue<String> R_Text;
        [Auto, Category("-Configuration - Text"), Description("Text edge padding")]
        public SimplePropertyValue<DistanceProperty> EdgePadding;
        [Auto, Category("-Configuration - Text"), Description("Text prefix")]
        public SimplePropertyValue<String> Prefix;
        [Auto, Category("-Configuration - Text"), Description("Text suffix")]
        public SimplePropertyValue<String> Suffix;
        #endregion

        #region Events
        [Auto] void OnVisualAdded(Visual sender) {
            if (sender.Parent is SceneVisual) { Utilities.SnapToPanel(sender); }
        }

        [Auto] protected void OnReset(Visual sender) { Setup(sender); }

        private void Setup(Visual sender) {
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // update connector
            UpdateConnector(sender);
            // update display value
            UpdateR_Text(sender);
        }

        [Auto] protected void OnR_TextUpdated(Visual sender, String value, String oldValue) { UpdateR_Text(sender); }
        [Auto] protected void OnEdgePaddingUpdated(Visual sender, DistanceProperty value, DistanceProperty oldValue) { UpdateR_Text(sender); }
        [Auto] protected void OnPrefixUpdated(Visual sender, String value, String oldValue) { UpdateR_Text(sender); }
        [Auto] protected void OnSuffixUpdated(Visual sender, String value, String oldValue) { UpdateR_Text(sender); }

        private void UpdateR_Text(Visual visual) {
            var sender = visual as BoxVisual;
            // update display text
            var displayText = sender.FindChild("DisplayText") as TextVisual;
            if (displayText.HorizontalAlign == HorizontalAlign.Right) {
                displayText.Location = vector(sender.Width / 2 - EdgePadding.Value, 0.0, -sender.Depth / 2 - displayText.Depth / 2 - 0.0002);
            }
            else if (displayText.HorizontalAlign == HorizontalAlign.Left) {
                displayText.Location = vector(-sender.Width / 2 + EdgePadding.Value, 0.0, -sender.Depth / 2 - displayText.Depth / 2 - 0.0002);
            }
            else if (displayText.HorizontalAlign == HorizontalAlign.Center) {
                displayText.Location = vector(0.0, 0.0, -sender.Depth / 2 - displayText.Depth / 2 - 0.0002);
            }
            displayText.Text = Prefix.Value + R_Text.Value + Suffix.Value;
        }

        [Auto] protected void OnAfterPropertyUpdated(Visual sender, String name) {
            if (name == "Depth" || name == "Height" || name == "Width" || name == "HorizontalAlign" || name == "LineHeight") {
                // update connector
                UpdateConnector(sender);
                // update display value
                UpdateR_Text(sender);
            }
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