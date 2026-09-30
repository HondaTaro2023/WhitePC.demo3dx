using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using System.Drawing;
using Demo3D.Utilities;
using System.Reflection;
using Demo3D.WiringDiagram;

// Created by Mark Emeott
// Updated September 15, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class SelectorSwitch : NativeObject {
        public SelectorSwitch(Visual sender) : base(sender) { Setup(sender); }
        
        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - Connector"), Description("Connector enabled and visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorEnabled;
        [Auto, Category("-Configuration - Contacts"), Description("Number of normally open contacts"), DefaultValue(1)]
        public SimplePropertyValue<Int32> NOContacts;
        [Auto, Category("-Configuration - Contacts"), Description("Number of normally closed contacts"), DefaultValue(1)]
        public SimplePropertyValue<Int32> NCContacts;
        [Auto, Category("-Configuration - Light"), Description("Self lighting output property")]
        public SimplePropertyValue<CustomEnumeration> SelfLightingOutput;
        [Auto, Category("-Configuration - Switch"), Description("Selector switch spring return (only for 2 position selector switch)")]
        public SimplePropertyValue<Boolean> SpringReturn;
        [Auto, Category("-Configuration - Switch"), Description("Selector switch selection time (if using spring return)"), DefaultValue(1.0)]
        public CustomPropertyValue<TimeProperty> SelectTime;
        [Auto, Category("-Configuration - Switch"), Description("Selector switch rotate speed"), DefaultValue(600.0)]
        public SimplePropertyValue<AngularSpeedProperty> RotateSpeed;
        [Auto, Category("-Configuration - Switch"), Description("Number of selector switch positions (2-7)"), DefaultValue(2)]
        public SimplePropertyValue<Int32> NumberOfPositions;
        [Auto, Category("-Configuration - Nametag"), Description("Show nametag"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ShowNametag;
        [Auto, Category("-State - Switch"), ReadOnly(true), Description("Selector switch current position")]
        public SimplePropertyValue<Int32> SwitchPosition;
        [Auto, Category("-Wiring In"), Description("Selector switch light power")]
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
            // clamp switch corner radius
            Utilities.ClampCornerRadius(sender.FindImmediateChild("Switch") as BoxTubeVisual);
            // setup normally open contact listeners
            var contactNumberNO = 0;
            for (int i = 1; i <= NumberOfPositions.Value; i++) {
                for (int j = 1; j <= NOContacts.Value; j++) {
                    contactNumberNO++;
                    var cp = sender.GetCustomProperty("R_NO" + contactNumberNO + "_Pos" + i );
                    if (cp != null) {
                        cp.UpdatedScript.NativeListeners -= CalculateResult;
                        cp.UpdatedScript.NativeListeners += CalculateResult;
                    }
                }
            }
            // setup normally closed contact listeners
            var contactNumberNC = 0;
            for (int i = 1; i <= NumberOfPositions.Value; i++) {
                for (int j = 1; j <= NCContacts.Value; j++) {
                    contactNumberNC++;
                    var cp = sender.GetCustomProperty("R_NC" + contactNumberNC + "_Pos" + i);
                    if (cp != null) {
                        cp.UpdatedScript.NativeListeners -= CalculateResult;
                        cp.UpdatedScript.NativeListeners += CalculateResult;
                    }
                }
            }
            // initialize self lighting output enum
            var e = SelfLightingOutput.Value;
            if (!e.AllowedValues.Contains("None")) {
                e.AllowedValues.Add("None");
                e.Value = "None";
            }
            contactNumberNO = 0;
            for (int i = 1; i <= NumberOfPositions.Value; i++) {
                for (int j = 1; j <= NOContacts.Value; j++) {
                    contactNumberNO++;
                    if (!e.AllowedValues.Contains("W_NO" + contactNumberNO + "_Pos" + i)) {
                        e.AllowedValues.Add("W_NO" + contactNumberNO + "_Pos" + i);
                    }
                }
            }
            contactNumberNC = 0;
            for (int i = 1; i <= NumberOfPositions.Value; i++) {
                for (int j = 1; j <= NCContacts.Value; j++) {
                    contactNumberNC++;
                    if (!e.AllowedValues.Contains("W_NC" + contactNumberNC + "_Pos" + i)) {
                        e.AllowedValues.Add("W_NC" + contactNumberNC + "_Pos" + i);
                    }
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
            // refresh wiring diagram & properties grid
            sender.AutoSizeHeightInWiringDiagram();
            sender.CompressPropertiesOnRails();
            WiringActions.RefreshWiringDiagram(document);
            Utilities.RefreshPropertiesGrid(sender);
        }

        [Auto] protected void OnShowNametagUpdated(Visual sender, Boolean value, Boolean oldValue) {
            var nametag = sender.FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Visible = ShowNametag.Value;
            }
        }

        [Auto] protected void OnClick(Visual sender, PickInfo info) {
            // increment switch position (in VR ActualVisualPicked is null)
            if (info.ActualVisualPicked == sender.FindChild("Switch") || info.ActualVisualPicked == sender.FindChild("IndicatorLight") || info.ActualVisualPicked == sender.FindChild("InnerRing") || info.ActualVisualPicked == null) {
                // positive increment
                SwitchPosition.Value = (SwitchPosition.Value >= NumberOfPositions.Value) ? 1 : SwitchPosition.Value + 1;
            }
            else if (info.ActualVisualPicked.Type == "TextVisual" && info.ActualVisualPicked.Name.StartsWith("TextPosition")) {
                // try to move to selected text position
                try {
                    var pos = Convert.ToInt32(info.ActualVisualPicked.Name.Replace("TextPosition", ""));
                    SwitchPosition.Value = pos;
                }
                catch (Exception) { }
            }
        }

        [Auto] protected IEnumerable OnSwitchPositionUpdated(Visual sender, Int32 value, Int32 oldValue) {
            // don't allow invalid switch position to be entered
            if (SwitchPosition.Value < 1) { SwitchPosition.Value = 1; yield return null; }
            else if (SwitchPosition.Value > NumberOfPositions.Value) { SwitchPosition.Value = NumberOfPositions.Value; yield return null; }
            // calculate result
            CalculateResult(sender, null, null);
            // calculate switch rotation
            var _switch = sender.FindImmediateChild("Switch");
            Double switchRotation = 0.0;
            if      (NumberOfPositions.Value == 2) { switchRotation = -125.0 + (SwitchPosition.Value - 1) * 70.0; }
            else if (NumberOfPositions.Value == 3) { switchRotation = -135.0 + (SwitchPosition.Value - 1) * 45.0; }
            else if (NumberOfPositions.Value == 4) { switchRotation = -157.5 + (SwitchPosition.Value - 1) * 45.0; }
            else if (NumberOfPositions.Value == 5) { switchRotation = -180.0 + (SwitchPosition.Value - 1) * 45.0; }
            else if (NumberOfPositions.Value == 6) { switchRotation = -202.5 + (SwitchPosition.Value - 1) * 45.0; }
            else if (NumberOfPositions.Value == 7) { switchRotation = -225.0 + (SwitchPosition.Value - 1) * 45.0; }
            if (app.Running) {
                // animate switch rotation
                _switch.RotateToLocalZ(-switchRotation + 90, RotateSpeed.Value);
                if (NumberOfPositions.Value == 2 && SpringReturn.Value) {
                    CylinderVisual outerRing = sender.FindChild("OuterRing") as CylinderVisual;
                    if (SwitchPosition.Value == 2) {
                        // spring return switch after delay
                        outerRing.Material.Color = Color.Silver;
                        yield return Wait.ForSeconds(SelectTime.Value);
                        SwitchPosition.Value = 1;
                    }
                    else {
                        outerRing.Material.Color = Color.Gray;
                    }
                }
            }
            else {
                // warp switch to rotation immediately
                _switch.RotationDegrees = vector(switchRotation, 90, 90);
            }
            // refresh properties grid
            Utilities.RefreshPropertiesGrid(sender);
        }

        [Auto] protected void OnNumberOfPositionsUpdated(Visual visual, Int32 value, Int32 oldValue) {
            var sender = visual as BoxVisual;
            // only allow 2 position to 7 position selector switch
            if (NumberOfPositions.Value < 2) { NumberOfPositions.Value = 2; return; }
            else if (NumberOfPositions.Value > 7) { NumberOfPositions.Value = 7; return; }
            // update text positions & show/hide spring return simple property alias
            var textPos1 = sender.FindImmediateChild("TextPosition1") as TextVisual;
            var textPos2 = sender.FindImmediateChild("TextPosition2") as TextVisual;
            var textPos3 = sender.FindImmediateChild("TextPosition3") as TextVisual;
            var textPos4 = sender.FindImmediateChild("TextPosition4") as TextVisual;
            var textPos5 = sender.FindImmediateChild("TextPosition5") as TextVisual;
            var textPos6 = sender.FindImmediateChild("TextPosition6") as TextVisual;
            var textPos7 = sender.FindImmediateChild("TextPosition7") as TextVisual;
            // setup scale factors so component can be resized with scaler tool
            var originalX = 0.049;
            var originalY = 0.049;
            var originalZ = 0.002;
            var originalTextLocZ = -0.0013;
            var scaleX = sender.Width / originalX;
            var scaleY = sender.Height / originalY;
            var scaleZ = sender.Depth / originalZ;
            if (NumberOfPositions.Value == 2) {
                textPos1.Location = vector(-0.010 * scaleX, 0.013 * scaleY, originalTextLocZ * scaleZ);
                textPos2.Location = vector( 0.010 * scaleX, 0.013 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Visible = false;
                textPos4.Visible = false;
                textPos5.Visible = false;
                textPos6.Visible = false;
                textPos7.Visible = false;
                sender.SimpleProperties.RemoveAliasByName("TextPosition3");
                sender.SimpleProperties.RemoveAliasByName("TextPosition4");
                sender.SimpleProperties.RemoveAliasByName("TextPosition5");
                sender.SimpleProperties.RemoveAliasByName("TextPosition6");
                sender.SimpleProperties.RemoveAliasByName("TextPosition7");
                if (sender.SimpleProperties.FindAliasForProperty(SpringReturn.Name) == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, SpringReturn.Name));
                }
            }
            else if (NumberOfPositions.Value == 3) {
                textPos1.Location = vector(-0.015 * scaleX, 0.0093 * scaleY, originalTextLocZ * scaleZ);
                textPos2.Location = vector( 0.000 * scaleX, 0.0136 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Location = vector( 0.015 * scaleX, 0.0093 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Visible = true;
                textPos4.Visible = false;
                textPos5.Visible = false;
                textPos6.Visible = false;
                textPos7.Visible = false;
                if (sender.SimpleProperties.FindAliasByName("TextPosition3") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos3.Name, "Text" }, "TextPosition3"));
                }
                sender.SimpleProperties.RemoveAliasByName("TextPosition4");
                sender.SimpleProperties.RemoveAliasByName("TextPosition5");
                sender.SimpleProperties.RemoveAliasByName("TextPosition6");
                sender.SimpleProperties.RemoveAliasByName("TextPosition7");
                sender.SimpleProperties.RemoveAliasByName(SpringReturn.Name);
            }
            else if (NumberOfPositions.Value == 4) {
                textPos1.Location = vector(-0.0189 * scaleX, 0.0036 * scaleY, originalTextLocZ * scaleZ);
                textPos2.Location = vector(-0.0086 * scaleX, 0.0129 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Location = vector( 0.0086 * scaleX, 0.0129 * scaleY, originalTextLocZ * scaleZ);
                textPos4.Location = vector( 0.0189 * scaleX, 0.0036 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Visible = true;
                textPos4.Visible = true;
                textPos5.Visible = false;
                textPos6.Visible = false;
                textPos7.Visible = false;
                if (sender.SimpleProperties.FindAliasByName("TextPosition3") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos3.Name, "Text" }, "TextPosition3"));
                }
                if (sender.SimpleProperties.FindAliasByName("TextPosition4") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos4.Name, "Text" }, "TextPosition4"));
                }
                sender.SimpleProperties.RemoveAliasByName("TextPosition5");
                sender.SimpleProperties.RemoveAliasByName("TextPosition6");
                sender.SimpleProperties.RemoveAliasByName("TextPosition7");
                sender.SimpleProperties.RemoveAliasByName(SpringReturn.Name);
            }
            else if (NumberOfPositions.Value == 5) {
                textPos1.Location = vector(-0.0196 * scaleX, -0.0036 * scaleY, originalTextLocZ * scaleZ);
                textPos2.Location = vector(-0.0150 * scaleX,  0.0093 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Location = vector( 0.0000 * scaleX,  0.0136 * scaleY, originalTextLocZ * scaleZ);
                textPos4.Location = vector( 0.0150 * scaleX,  0.0093 * scaleY, originalTextLocZ * scaleZ);
                textPos5.Location = vector( 0.0196 * scaleX, -0.0036 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Visible = true;
                textPos4.Visible = true;
                textPos5.Visible = true;
                textPos6.Visible = false;
                textPos7.Visible = false;
                if (sender.SimpleProperties.FindAliasByName("TextPosition3") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos3.Name, "Text" }, "TextPosition3"));
                }
                if (sender.SimpleProperties.FindAliasByName("TextPosition4") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos4.Name, "Text" }, "TextPosition4"));
                }
                if (sender.SimpleProperties.FindAliasByName("TextPosition5") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos5.Name, "Text" }, "TextPosition5"));
                }
                sender.SimpleProperties.RemoveAliasByName("TextPosition6");
                sender.SimpleProperties.RemoveAliasByName("TextPosition7");
                sender.SimpleProperties.RemoveAliasByName(SpringReturn.Name);
            }
            else if (NumberOfPositions.Value == 6) {
                textPos1.Location = vector(-0.0189 * scaleX, -0.0100 * scaleY, originalTextLocZ * scaleZ);
                textPos2.Location = vector(-0.0189 * scaleX,  0.0036 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Location = vector(-0.0086 * scaleX,  0.0129 * scaleY, originalTextLocZ * scaleZ);
                textPos4.Location = vector( 0.0086 * scaleX,  0.0129 * scaleY, originalTextLocZ * scaleZ);
                textPos5.Location = vector( 0.0189 * scaleX,  0.0036 * scaleY, originalTextLocZ * scaleZ);
                textPos6.Location = vector( 0.0189 * scaleX, -0.0100 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Visible = true;
                textPos4.Visible = true;
                textPos5.Visible = true;
                textPos6.Visible = true;
                textPos7.Visible = false;
                if (sender.SimpleProperties.FindAliasByName("TextPosition3") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos3.Name, "Text" }, "TextPosition3"));
                }
                if (sender.SimpleProperties.FindAliasByName("TextPosition4") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos4.Name, "Text" }, "TextPosition4"));
                }
                if (sender.SimpleProperties.FindAliasByName("TextPosition5") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos5.Name, "Text" }, "TextPosition5"));
                }
                if (sender.SimpleProperties.FindAliasByName("TextPosition6") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos6.Name, "Text" }, "TextPosition6"));
                }
                sender.SimpleProperties.RemoveAliasByName("TextPosition7");
                sender.SimpleProperties.RemoveAliasByName(SpringReturn.Name);
            }
            else if (NumberOfPositions.Value == 7) {
                textPos1.Location = vector(-0.0157 * scaleX, -0.0157 * scaleY, originalTextLocZ * scaleZ);
                textPos2.Location = vector(-0.0196 * scaleX, -0.0036 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Location = vector(-0.0150 * scaleX,  0.0093 * scaleY, originalTextLocZ * scaleZ);
                textPos4.Location = vector( 0.0000 * scaleX,  0.0136 * scaleY, originalTextLocZ * scaleZ);
                textPos5.Location = vector( 0.0150 * scaleX,  0.0093 * scaleY, originalTextLocZ * scaleZ);
                textPos6.Location = vector( 0.0196 * scaleX, -0.0036 * scaleY, originalTextLocZ * scaleZ);
                textPos7.Location = vector( 0.0157 * scaleX, -0.0157 * scaleY, originalTextLocZ * scaleZ);
                textPos3.Visible = true;
                textPos4.Visible = true;
                textPos5.Visible = true;
                textPos6.Visible = true;
                textPos7.Visible = true;
                if (sender.SimpleProperties.FindAliasByName("TextPosition3") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos3.Name, "Text" }, "TextPosition3"));
                }
                if (sender.SimpleProperties.FindAliasByName("TextPosition4") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos4.Name, "Text" }, "TextPosition4"));
                }
                if (sender.SimpleProperties.FindAliasByName("TextPosition5") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos5.Name, "Text" }, "TextPosition5"));
                }
                if (sender.SimpleProperties.FindAliasByName("TextPosition6") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos6.Name, "Text" }, "TextPosition6"));
                }
                if (sender.SimpleProperties.FindAliasByName("TextPosition7") == null) {
                    sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, new string[] { "Children", textPos7.Name, "Text" }, "TextPosition7"));
                }
                sender.SimpleProperties.RemoveAliasByName(SpringReturn.Name);
            }
            // update switch position (bounce through a valid position so the rotation recalculates for the new position count, without ever writing an invalid value)
            var currentPosition = SwitchPosition.Value;
            var newPosition = Math.Min(currentPosition, NumberOfPositions.Value);
            if (newPosition == currentPosition) {
                SwitchPosition.Value = (newPosition == 1) ? 2 : 1;
            }
            SwitchPosition.Value = newPosition;
            // re-create contacts
            RecreateContacts(sender);
        }

        [Auto] protected void OnNOContactsUpdated(Visual sender, Int32 value, Int32 oldValue) { RecreateContacts(sender); }

        [Auto] protected void OnNCContactsUpdated(Visual sender, Int32 value, Int32 oldValue) { RecreateContacts(sender); }

        private void RecreateContacts(Visual sender) {
            // flag all normally open contacts for removal
            List<string> removalsNO = new List<string>();
            CustomPropertyCollection propsNO = sender.CustomProperties;
            foreach (CustomProperty prop in propsNO) {
                if (prop.Name.StartsWith("R_NO") || prop.Name.StartsWith("W_NO")) {
                    removalsNO.Add(prop.Name);
                }
            }
            // remove all flagged normally open contacts
            foreach (var removal in removalsNO) {
                if (sender.HasCustomProperty(removal)) {
                    var cp = sender.GetCustomProperty(removal);
                    cp.RemoveFromWiringDiagram();
                    sender.RemoveCustomProperty(removal);
                    // remove from self lighting output enum
                    var e = SelfLightingOutput.Value;
                    if (e.AllowedValues.Contains(removal)) {
                        e.AllowedValues.Remove(removal);
                    }
                }
            }
            // re-create needed normally open contacts
            var contactNumberNO = 0;
            for (int i = 1; i <= NumberOfPositions.Value; i++) {
                for (int j = 1; j <= NOContacts.Value; j++) {
                    contactNumberNO++;
                    if (!sender.HasCustomProperty("R_NO" + contactNumberNO + "_Pos" + i )) {
                        var cp = sender.AddSimpleProperty("R_NO" + contactNumberNO + "_Pos" + i , false, "Normally Open " + contactNumberNO + " Position " + i + " Wiring In");
                        cp.Category = "-Wiring In";
                        cp.UpdatedScript.NativeListeners -= CalculateResult;
                        cp.UpdatedScript.NativeListeners += CalculateResult;
                        cp.PlaceOnLeftRail();
                    }
                    if (!sender.HasCustomProperty("W_NO" + contactNumberNO + "_Pos" + i)) {
                        var cp = sender.AddSimpleProperty("W_NO" + contactNumberNO + "_Pos" + i, false, "Normally Open " + contactNumberNO + " Position " + i + " Wiring Out");
                        cp.Category = "-Wiring Out";
                        cp.ReadOnly = true;
                        cp.PlaceOnRightRail();
                        // add to self lighting output enum
                        var e = SelfLightingOutput.Value;
                        if (!e.AllowedValues.Contains("W_NO" + contactNumberNO + "_Pos" + i)) {
                            e.AllowedValues.Add("W_NO" + contactNumberNO + "_Pos" + i);
                        }
                    }
                }
            }
            // flag all normally closed contacts for removal
            List<string> removalsNC = new List<string>();
            CustomPropertyCollection propsNC = sender.CustomProperties;
            foreach (CustomProperty prop in propsNC) {
                if (prop.Name.StartsWith("R_NC") || prop.Name.StartsWith("W_NC")) {
                    removalsNC.Add(prop.Name);
                }
            }
            // remove all flagged normally closed contacts
            foreach (var removal in removalsNC) {
                if (sender.HasCustomProperty(removal)) {
                    var cp = sender.GetCustomProperty(removal);
                    cp.RemoveFromWiringDiagram();
                    sender.RemoveCustomProperty(removal);
                    // remove from self lighting output enum
                    var e = SelfLightingOutput.Value;
                    if (e.AllowedValues.Contains(removal)) {
                        e.AllowedValues.Remove(removal);
                    }
                }
            }
            // re-create needed normally closed contacts
            var contactNumberNC = 0;
            for (int i = 1; i <= NumberOfPositions.Value; i++) {
                for (int j = 1; j <= NCContacts.Value; j++) {
                    contactNumberNC++;
                    if (!sender.HasCustomProperty("R_NC" + contactNumberNC + "_Pos" + i)) {
                        var cp = sender.AddSimpleProperty("R_NC" + contactNumberNC + "_Pos" + i, false, "Normally Closed " + contactNumberNC + " Position " + i + " Wiring In");
                        cp.Category = "-Wiring In";
                        cp.UpdatedScript.NativeListeners -= CalculateResult;
                        cp.UpdatedScript.NativeListeners += CalculateResult;
                        cp.PlaceOnLeftRail();
                    }
                    if (!sender.HasCustomProperty("W_NC" + contactNumberNC + "_Pos" + i)) {
                        var cp = sender.AddSimpleProperty("W_NC" + contactNumberNC + "_Pos" + i, false, "Normally Closed " + contactNumberNC + " Position " + i + " Wiring Out");
                        cp.Category = "-Wiring Out";
                        cp.ReadOnly = true;
                        cp.PlaceOnRightRail();
                        // add to self lighting output enum
                        var e = SelfLightingOutput.Value;
                        if (!e.AllowedValues.Contains("W_NC" + contactNumberNC + "_Pos" + i)) {
                            e.AllowedValues.Add("W_NC" + contactNumberNC + "_Pos" + i);
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

        private void CalculateResult(Visual sender, object _value, object _oldValue) {
            // calculate normally open contact output values
            var contactNumberNO = 0;
            for (int i = 1; i <= NumberOfPositions.Value; i++) {
                for (int j = 1; j <= NOContacts.Value; j++) {
                    contactNumberNO++;
                    if (sender.HasCustomProperty("R_NO" + contactNumberNO + "_Pos" + i ) && sender.HasCustomProperty("W_NO" + contactNumberNO + "_Pos" + i)) {
                        var cpInput = sender.GetCustomProperty("R_NO" + contactNumberNO + "_Pos" + i );
                        var cpOutput = sender.GetCustomProperty("W_NO" + contactNumberNO + "_Pos" + i);
                        cpOutput.Value = (SwitchPosition.Value == i && (bool)cpInput.Value) ? true : false;
                        if (SelfLightingOutput.Value.Value == cpOutput.Name) {
                            sender.SetCustomPropertyValue<bool>("R_LightPower", (bool)cpOutput.Value);
                        }
                    }
                }
            }
            // calculate normally closed contact output values
            var contactNumberNC = 0;
            for (int i = 1; i <= NumberOfPositions.Value; i++) {
                for (int j = 1; j <= NCContacts.Value; j++) {
                    contactNumberNC++;
                    if (sender.HasCustomProperty("R_NC" + contactNumberNC + "_Pos" + i) && sender.HasCustomProperty("W_NC" + contactNumberNC + "_Pos" + i)) {
                        var cpInput = sender.GetCustomProperty("R_NC" + contactNumberNC + "_Pos" + i);
                        var cpOutput = sender.GetCustomProperty("W_NC" + contactNumberNC + "_Pos" + i);
                        if (!(bool)cpInput.Value) {
                            cpOutput.Value = false;
                        }
                        else {
                            cpOutput.Value = (SwitchPosition.Value == i && (bool)cpInput.Value) ? false : true;
                        }
                        if (SelfLightingOutput.Value.Value == cpOutput.Name) {
                            sender.SetCustomPropertyValue<bool>("R_LightPower", (bool)cpOutput.Value);
                        }
                    }
                }
            }
        }

        [Auto] protected void OnSpringReturnUpdated(Visual sender, Boolean value, Boolean oldValue) {
            if (value) {
                sender.SimpleProperties.Aliases.Add(new PropertyAlias(sender.SimpleProperties, "SelectTime"));
            }
            else {
                sender.SimpleProperties.Aliases.Remove(sender.SimpleProperties.FindAliasByName("SelectTime"));
            }
        }

        [Auto] protected void OnR_LightPowerUpdated(Visual sender, Boolean value, Boolean oldValue) {
            // pass light power signal from parent visual to child light visual
            var indicatorLight = sender.FindChild("IndicatorLight");
            indicatorLight.SetCustomPropertyValue("R_LightPower", sender.GetCustomPropertyValue("R_LightPower"));
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