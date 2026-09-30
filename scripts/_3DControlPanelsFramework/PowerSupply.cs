using System;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using System.Reflection;
using Demo3D.Utilities;
using System.Drawing;

// Created by Mark Emeott
// Updated June 30, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class PowerSupply : NativeObject {
        public PowerSupply(Visual sender) : base(sender) { Setup(sender); }

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - Forcing"), Description("Enable forcing such that clicking the status light (DC_OK) toggles the input power signal (R_AC)")]
        public SimplePropertyValue<Boolean> EnableForcing;
        [Auto, Category("-Configuration - Nametag"), Description("Show nametag"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ShowNametag;
        [Auto, Category("-Configuration - Connector"), Description("Connector enabled and visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorEnabled;
        [Auto, Category("-Wiring In"), Description("AC Power")]
        public SimplePropertyValue<Boolean> R_AC;
        [Auto, Category("-Wiring Out"), ReadOnly(true), Description("DC Power")]
        public SimplePropertyValue<Boolean> W_DC;
        #endregion

        #region Events
        [Auto] void OnVisualAdded(Visual sender) {
            if (sender.Parent is SceneVisual) { Utilities.SnapToPanel(sender); }
        }

        [Auto] protected void OnReset(Visual sender) {
            // disable forcing
            EnableForcing.Value = false;
            Setup(sender);
        }

        private void Setup(Visual sender) {
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // update connector
            UpdateConnector(sender);
        }

        [Auto] protected void OnShowNametagUpdated(Visual sender, Boolean value, Boolean oldValue) {
            var frontPlate = sender.FindChild("FrontPlate") as BoxVisual;
            if (frontPlate != null) {
                frontPlate.Visible = ShowNametag.Value;
            }
            var nametag = frontPlate.FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Visible = ShowNametag.Value;
            }
        }

        [Auto] protected void OnEnableForcingUpdated(Visual sender, Boolean value, Boolean oldValue) {
            var frontPlate = sender.FindImmediateChild("FrontPlate") as BoxVisual;
            if (frontPlate != null) {
                var r_ac = (BindableItem)R_AC;
                if (EnableForcing.Value) {
                    // force the input power signal & update front plate color
                    r_ac.Force(R_AC.Value);
                    frontPlate.Material.Color = Color.Orange;
                }
                else {
                    // unforce the input power signal & update front plate color
                    r_ac.UnForce();
                    frontPlate.Material.Color = Color.LightGray;
                }
            }
        }

        [Auto] protected void OnClick(Visual sender, PickInfo info) {
            if (EnableForcing.Value && info.ActualVisualPicked == sender.FindChild("DC_OK")) {
                // toggle the input power signal
                var r_ac = (BindableItem)R_AC;
                r_ac.Force(!R_AC.Value);
            }
        }

        [Auto] protected void OnR_ACUpdated(Visual sender, Boolean value, Boolean oldValue) {
            // pass ac power signal to dc power signal and update dc_ok light
            sender.SetCustomPropertyValue("W_DC", sender.GetCustomPropertyValue("R_AC"));
            var dc_ok = sender.FindChild("DC_OK");
            dc_ok.SetCustomPropertyValue("R_LightPower", sender.GetCustomPropertyValue("W_DC"));
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