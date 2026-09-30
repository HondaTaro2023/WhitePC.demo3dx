using System;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using System.Reflection;
using Demo3D.Utilities;

// Created by Mark Emeott
// Updated June 30, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class Nametag : NativeObject {
        public Nametag(Visual sender) : base(sender) { Setup(sender); }

        #region Custom Properties
        public enum eDisplayProperty { Name, Type, Custom }
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - Nametag"), Description("Nametag ancestor level\n0 = yourself\n1 = your parent\n2 = your parent's parent\n3...  higher ancestor levels")]
        public SimplePropertyValue<Byte> AncestorLevel;
        [Auto, Category("-Configuration - Nametag"), DefaultValue(eDisplayProperty.Name), Description("Nametag display property (Name, Type, or Custom)")]
        public SimplePropertyValue<eDisplayProperty> DisplayProperty;
        [Auto, Category("-Configuration - Nametag"), Description("Display property name")]
        public SimplePropertyValue<String> DisplayPropertyName;
        [Auto, Category("-Configuration - Connector"), Description("Connector enabled and visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorEnabled;
        #endregion

        #region Events
        [Auto] void OnVisualAdded(Visual sender) {
            if (sender.Parent is SceneVisual) { Utilities.SnapToPanel(sender); }
        }

        [Auto] protected void OnReset(Visual sender) { Setup(sender); }

        private void Setup(Visual sender) {
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // update nametag
            UpdateNametag(sender);
            // update connector
            UpdateConnector(sender);
        }

        [Auto] protected void OnParentUpdated(Visual sender, Visual oldParent, Visual newParent) {
            UpdateConnector(sender);
            UpdateNametag(sender);
        }

        [Auto] protected void OnAncestorLevelUpdated(Visual sender, Byte value, Byte oldValue) { UpdateNametag(sender); }
        [Auto] protected void OnDisplayPropertyUpdated(Visual sender, eDisplayProperty value, eDisplayProperty oldValue) { UpdateNametag(sender); }
        [Auto] protected void OnDisplayPropertyNameUpdated(Visual sender, String value, String oldValue) { UpdateNametag(sender); }

        [Auto] protected void OnAfterPropertyUpdated(Visual sender, String name) {
            if(name == "Name" || name == "FullName" || name == "Parent") {
                UpdateNametag(sender);
            }
        }

        private void UpdateNametag(Visual visual) {
            // find specified ancestor visual
            TextVisual sender = visual as TextVisual;
            Visual ancestor = sender;
            var i = 0;
            while (i < AncestorLevel.Value && ancestor != document.Scene) {
                i++;
                ancestor = ancestor.Parent;
            }
            // update nametag text
            if (DisplayProperty.Value == eDisplayProperty.Custom) {
                // show custom property
                DisplayPropertyName.Hidden = false;
                // attempt to display specified custom property value
                try {
                    sender.Text = ancestor.GetProperty(DisplayPropertyName.Value.ToString()).ToString();
                }
                catch (Exception) {
                    sender.Text = "";
                }
            }
            else {
                // hide custom property
                DisplayPropertyName.Hidden = true;
                // display name or type
                sender.Text = ancestor.GetProperty(DisplayProperty.Value.ToString()).ToString();
            }
            // update connector
            UpdateConnector(sender);
            // refresh properties grid
            Utilities.RefreshPropertiesGrid(sender);
        }

        [Auto] protected void OnConnectorEnabledUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnector(sender); }
        #endregion

        #region Connector Configuration
        private void UpdateConnector(Visual visual) {
            // create panel connector on back
            TextVisual sender = visual as TextVisual;
            Connector c = sender.FindCreateConnector("C1");
            c.Start = vector(0.0, 0.0, sender.Depth / 2 + 0.0001);
            c.End = vector(-0.00001, 0.0, sender.Depth / 2 + 0.0001);
            c.Normal = vector(0, 0, 1);
            c.Type = "PanelComponent";
            c.Allowed = new string[] { "Panel" };
            c.ReparentOnConnect = true;
            c.AlignmentStyle = ConnectorAlignmentStyle.Complete;
            c.MaxAllowedConnections = 1;
            c.AutoConnect = (ConnectorEnabled.Value) ? true : false;
            c.ControlPointEnabled = (ConnectorEnabled.Value) ? true : false;
            c.ControlPointSize = sender.LineHeight / 10;
            c.TextHeight = sender.LineHeight / 10;
            // set snap distance
            var xSpacing = sender.LineHeight;
            var ySpacing = sender.LineHeight;
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