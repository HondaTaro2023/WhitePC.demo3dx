using System;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using System.Drawing;
using System.Reflection;
using Demo3D.Utilities;
using System.Linq;

// Created by Mark Emeott
// Updated September 15, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class Lock : NativeObject {
        public Lock(Visual sender) : base(sender) { Setup(sender); }

        #region Visuals
        [Auto] BoxTubeVisual Shackle;
        #endregion

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - Nametag"), Description("Show nametag"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ShowNametag;
        [Auto, Category("-Configuration - Connector"), Description("Connector enabled and visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorEnabled;
        [Auto, Category("-State - Lock"), ReadOnly(true), Description("Panel door locked")]
        public SimplePropertyValue<Boolean> Locked;
        #endregion

        #region Events
        [Auto] void OnVisualAdded(Visual sender) {
            if (sender.Parent is SceneVisual) { Utilities.SnapToPanel(sender); }
        }

        [Auto] protected void OnReset(Visual sender) { Setup(sender); }

        private void Setup(Visual sender) {
            // update visual references
            Shackle = sender.Children.Where(v => v.Type == "Shackle").FirstOrDefault() as BoxTubeVisual;
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // update connector
            UpdateConnector(sender);
            // remove old locked property
            if (sender.HasCustomProperty("I_Locked")) {
                Locked.Value = sender.GetCustomPropertyValue<bool>("I_Locked");
                sender.RemoveCustomProperty("I_Locked");
            }
            // clamp shackle corner radius
            Utilities.ClampCornerRadius(Shackle);
        }

        [Auto] protected void OnShowNametagUpdated(Visual sender, Boolean value, Boolean oldValue) {
            var nametag = sender.FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Visible = ShowNametag.Value;
            }
        }

        [Auto] protected void OnClick(Visual sender, PickInfo info) {
            // respond to click (in VR ActualVisualPicked is null)
            Locked.Value = !Locked.Value;
        }

        [Auto] protected void OnLockedUpdated(Visual visual, Boolean value, Boolean oldValue) {
            var sender = visual as BoxTubeVisual;
            // update lock color
            sender.EndYmax.Material.Color = Locked.Value ? Color.DarkRed : Color.Green;
            // update shackle
            Shackle.SideXmaxVisible = Locked.Value ? true : false;
            // refresh properties grid
            Utilities.RefreshPropertiesGrid(sender);
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
            BoxTubeVisual sender = visual as BoxTubeVisual;
            Demo3D.Visuals.Connector c = sender.FindCreateConnector("C1");
            c.Start = vector(0.0, -sender.Height / 2, 0.0);
            c.End = vector(-0.00001, -sender.Height / 2, 0.0);
            c.Normal = vector(0, -1, 0);
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