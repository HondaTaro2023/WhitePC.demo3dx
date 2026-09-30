using System;
using System.Collections;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using System.Drawing;
using Demo3D.Utilities;
using System.Reflection;
using Demo3D.WiringDiagram;

// Created by Mark Emeott
// Updated June 30, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class PushButton : NativeObject {
        public PushButton(Visual sender) : base(sender) { Setup(sender); }

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        public enum eButtonType { Toggle, Momentary, MomentaryTimeout }
        [Auto, Category("-Configuration - Button"), Description("Button type (Toggle, Momentary, or MomentaryTimeout)"), DefaultValue(eButtonType.Momentary)]
        public SimplePropertyValue<eButtonType> ButtonType;
        [Auto, Category("-Configuration - Button"), Description("Push button momentary timeout time"), DefaultValue(1.0)]
        public CustomPropertyValue<TimeProperty> MomentaryTimeout;
        [Auto, Category("-Configuration - Button"), Description("Push button push speed"), DefaultValue(0.1)]
        public SimplePropertyValue<SpeedProperty> PushSpeed;
        [Auto, Category("-Configuration - Connector"), Description("Connector enabled and visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorEnabled;
        [Auto, Category("-Configuration - Contacts"), Description("Number of normally open contacts"), DefaultValue(1)]
        public SimplePropertyValue<Byte> NOContacts;
        [Auto, Category("-Configuration - Contacts"), Description("Number of normally closed contacts"), DefaultValue(1)]
        public SimplePropertyValue<Byte> NCContacts;
        [Auto, Category("-Configuration - Light"), Description("Self lighting output property")]
        public SimplePropertyValue<CustomEnumeration> SelfLightingOutput;
        [Auto, Category("-Configuration - Nametag"), Description("Show nametag"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ShowNametag;
        [Auto, Category("-State - Button"), ReadOnly(true), Description("Push button pressed")]
        public SimplePropertyValue<Boolean> ButtonPressed;
        [Auto, Category("-State - Button"), ReadOnly(true), Description("Push button not pressed"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ButtonNotPressed;
        [Auto, Category("-Wiring In"), Description("Push button light power")]
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
            // initialize self lighting output enum
            var e = SelfLightingOutput.Value;
            if (!e.AllowedValues.Contains("None")) {
                e.AllowedValues.Add("None");
                e.Value = "None";
            }
            if (!e.AllowedValues.Contains("ButtonPressed")) {
                e.AllowedValues.Add("ButtonPressed");
            }
            if (!e.AllowedValues.Contains("ButtonNotPressed")) {
                e.AllowedValues.Add("ButtonNotPressed");
            }
            for (int i = 1; i <= NOContacts.Value; i++) {
                if (!e.AllowedValues.Contains("W_NO" + i)) {
                    e.AllowedValues.Add("W_NO" + i);
                }
            }
            for (int i = 1; i <= NCContacts.Value; i++) {
                if (!e.AllowedValues.Contains("W_NC" + i)) {
                    e.AllowedValues.Add("W_NC" + i);
                }
            }
        }

        [Auto] protected void OnSelfLightingOutputUpdated(Visual sender, CustomEnumeration value, CustomEnumeration oldValue) {
            var lightPower = sender.GetCustomProperty("R_LightPower");
            var e = SelfLightingOutput.Value;
            if (e.Value == "None") {
                // not self lighted, add light power back to properties grid & wiring diagram rail
                lightPower.Value = false;
                lightPower.ReadOnly = false;
                lightPower.Hidden = false;
                lightPower.PlaceOnLeftRail(0.0);
            }
            else if (!e.AllowedValues.Contains(e.Value)) {
                // invalid self lighting output value, reset value
                e.Value = "None";
            }
            else {
                // self lighted, remove light power from properties grid & wiring diagram rail
                lightPower.ReadOnly = true;
                lightPower.Hidden = true;
                lightPower.RemoveFromWiringDiagram();
            }
            if (value == null) {
                // function called by a NOContacts or NCContacts update, so no calculations or updates needed
                return;
            }
            // calculate result
            CalculateResult(sender, null, null);
            // calculate self lighting
            CalculateSelfLighting(sender, "ButtonPressed");
            CalculateSelfLighting(sender, "ButtonNotPressed");
            // refresh wiring diagram & properties grid
            sender.AutoSizeHeightInWiringDiagram();
            sender.CompressPropertiesOnRails();
            WiringActions.RefreshWiringDiagram(document);
            Utilities.RefreshPropertiesGrid(sender);
        }

        private void CalculateSelfLighting(Visual sender, string name) {
            // if property name used for self lighting, update light power with looked up value
            if (SelfLightingOutput.Value.Value == name) {
                sender.SetCustomPropertyValue<bool>("R_LightPower", sender.GetCustomPropertyValue<bool>(name));
            }
        }

        private void CalculateSelfLighting(Visual sender, string name, bool value) {
            // if property name used for self lighting, update light power with passed in value
            if (SelfLightingOutput.Value.Value == name) {
                sender.SetCustomPropertyValue<bool>("R_LightPower", value);
            }
        }

        [Auto] protected void OnShowNametagUpdated(Visual sender, Boolean value, Boolean oldValue) {
            var nametag = sender.FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Visible = ShowNametag.Value;
            }
        }

        [Auto] protected void OnClick(Visual sender, PickInfo info) {
            // respond to click (in VR ActualVisualPicked is null)
            if (info.ActualVisualPicked == sender.FindChild("ButtonLight") || info.ActualVisualPicked == null) {
                if(ButtonType == eButtonType.Toggle) {
                    // toggle button
                    ButtonPressed.Value = !ButtonPressed.Value;
                }
                else if (ButtonType == eButtonType.Momentary || ButtonType == eButtonType.MomentaryTimeout) {
                    // depress button
                    ButtonPressed.Value = true;
                }
            }
        }

        [Auto] protected void OnMouseUp(Visual sender, PickInfo info) {
            if (ButtonType == eButtonType.Momentary || (!app.Running && ButtonType == eButtonType.MomentaryTimeout)) {
                // unpress button
                ButtonPressed.Value = false;
            }
        }

        [Auto] protected IEnumerable OnButtonPressedUpdated(Visual visual, Boolean value, Boolean oldValue) {
            var sender = visual as BoxVisual;
            // update not value & calculate self lighting
            ButtonNotPressed.Value = !ButtonPressed.Value;
            CalculateSelfLighting(sender, "ButtonPressed");
            CalculateSelfLighting(sender, "ButtonNotPressed");
            // calculate result
            CalculateResult(sender, null, null);
            // update outer ring color
            CylinderVisual outerRing = sender.FindChild("OuterRing") as CylinderVisual;
            outerRing.Material.Color = (value) ? Color.Silver : Color.Gray;
            // update button location (setup scale factors so component can be resized with scaler tool)
            var originalX = 0.049;
            var originalY = 0.049;
            var originalZ = 0.002;
            var scaleX = sender.Width / originalX;
            var scaleY = sender.Height / originalY;
            var scaleZ = sender.Depth / originalZ;
            var onLocation =  vector(0 * scaleX, -0.0035 * scaleY, -0.0005 * scaleZ);
            var offLocation = vector(0 * scaleX, -0.0035 * scaleY, -0.0040 * scaleZ);
            var pushLocation = value ? onLocation : offLocation;
            var buttonLight = sender.FindImmediateChild("ButtonLight");
            if (app.Running) {
                // animate button movement
                buttonLight.MoveTo(sender, pushLocation, PushSpeed.Value);
                if (ButtonType == eButtonType.MomentaryTimeout && ButtonPressed.Value) {
                    // unpress button after delay
                    yield return Wait.ForSeconds(MomentaryTimeout.Value);
                    ButtonPressed.Value = false;
                }
            }
            else {
                // warp button to location immediately
                buttonLight.Location = pushLocation;
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
                        // add to self lighting output enum
                        var e = SelfLightingOutput.Value;
                        if (!e.AllowedValues.Contains("W_NO" + i)) {
                            e.AllowedValues.Add("W_NO" + i);
                        }
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
                        // remove from self lighting output enum
                        var e = SelfLightingOutput.Value;
                        if (e.AllowedValues.Contains("W_NO" + i)) {
                            e.AllowedValues.Remove("W_NO" + i);
                        }
                    }
                }
            }
            // check self lighting output property
            OnSelfLightingOutputUpdated(sender, null, null);
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
                        // add to self lighting output enum
                        var e = SelfLightingOutput.Value;
                        if (!e.AllowedValues.Contains("W_NC" + i)) {
                            e.AllowedValues.Add("W_NC" + i);
                        }
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
                        // remove from self lighting output enum
                        var e = SelfLightingOutput.Value;
                        if (e.AllowedValues.Contains("W_NC" + i)) {
                            e.AllowedValues.Remove("W_NC" + i);
                        }
                    }
                }
            }
            // check self lighting output property
            OnSelfLightingOutputUpdated(sender, null, null);
            // calculate result
            CalculateResult(sender, null, null);
            // refresh wiring diagram & properties grid
            sender.AutoSizeHeightInWiringDiagram();
            sender.CompressPropertiesOnRails();
            WiringActions.RefreshWiringDiagram(document);
            Utilities.RefreshPropertiesGrid(sender);
        }

        private void CalculateResult(Visual sender, object _value, object _oldValue) {
            // calculate normally open contact output values
            for (int i = 1; i <= NOContacts.Value; i++) {
                if (sender.HasCustomProperty("R_NO" + i) && sender.HasCustomProperty("W_NO" + i)) {
                    var result = (Boolean)sender.GetCustomPropertyValue("R_NO" + i) && ButtonPressed.Value;
                    sender.SetCustomPropertyValue("W_NO" + i, result);
                    CalculateSelfLighting(sender, "W_NO" + i, result);
                }
            }
            // calculate normally closed contact output values
            for (int i = 1; i <= NCContacts.Value; i++) {
                if (sender.HasCustomProperty("R_NC" + i) && sender.HasCustomProperty("W_NC" + i)) {
                    var result = (Boolean)sender.GetCustomPropertyValue("R_NC" + i) && !ButtonPressed.Value;
                    sender.SetCustomPropertyValue("W_NC" + i, result);
                    CalculateSelfLighting(sender, "W_NC" + i, result);
                }
            }
        }

        [Auto] protected void OnButtonTypeUpdated(Visual sender, eButtonType value, eButtonType oldValue) {
            if (value == eButtonType.MomentaryTimeout) {
                // add alias to simple properties
                sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, "MomentaryTimeout"));
            }
            else if(value == eButtonType.Toggle || value == eButtonType.Momentary){
                // remove alias from simple properties
                sender.SimpleProperties.Aliases.Remove(sender.SimpleProperties.FindAliasByName("MomentaryTimeout"));
            }
            Utilities.RefreshPropertiesGrid(sender);
        }

        [Auto] protected void OnR_LightPowerUpdated(Visual sender, Boolean value, Boolean oldValue) {
            // pass light power signal from parent visual to child light visual
            var buttonLight = sender.FindChild("ButtonLight");
            buttonLight.SetCustomPropertyValue("R_LightPower", sender.GetCustomPropertyValue("R_LightPower"));
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