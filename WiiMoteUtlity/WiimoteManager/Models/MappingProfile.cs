using CommunityToolkit.Mvvm.ComponentModel;
using WiimoteManager.Models;

namespace WiimoteManager.Models
{
    /// <summary>
    /// Defines which Wiimote input triggers an Xbox action
    /// </summary>
    public partial class ControlMapping : ObservableObject
    {
        [ObservableProperty]
        public string targetName = string.Empty; 
        
        // Source: Wiimote
        [ObservableProperty]
        public ButtonState? wiimoteButton;
        
        // For future: Axis mapping (e.g. AccelX -> RightStickX)
        // public bool IsAxis { get; set; }
        // public string AxisName { get; set; }

        public override string ToString() => $"{TargetName}: {WiimoteButton}";
    }

    /// <summary>
    /// Complete profile mapping Wiimote inputs to Xbox 360 outputs
    /// </summary>
    public class MappingProfile
    {
        public string Name { get; set; } = "Default";

        // Standard Buttons
        public ControlMapping A { get; set; } = new() { TargetName = "A" };
        public ControlMapping B { get; set; } = new() { TargetName = "B" };
        public ControlMapping X { get; set; } = new() { TargetName = "X" };
        public ControlMapping Y { get; set; } = new() { TargetName = "Y" };
        
        // Bumpers
        public ControlMapping LeftShoulder { get; set; } = new() { TargetName = "LB" };
        public ControlMapping RightShoulder { get; set; } = new() { TargetName = "RB" };
        
        // Special
        public ControlMapping Start { get; set; } = new() { TargetName = "Start" };
        public ControlMapping Back { get; set; } = new() { TargetName = "Back" };
        public ControlMapping Guide { get; set; } = new() { TargetName = "Guide" };
        
        // Sticks (Clicks)
        public ControlMapping LeftThumb { get; set; } = new() { TargetName = "L-Click" };
        public ControlMapping RightThumb { get; set; } = new() { TargetName = "R-Click" };
        
        // D-Pad
        public ControlMapping DPadUp { get; set; } = new() { TargetName = "DPad Up" };
        public ControlMapping DPadDown { get; set; } = new() { TargetName = "DPad Down" };
        public ControlMapping DPadLeft { get; set; } = new() { TargetName = "DPad Left" };
        public ControlMapping DPadRight { get; set; } = new() { TargetName = "DPad Right" };

        // Helper to iterate
        public IEnumerable<ControlMapping> AllMappings => new[]
        {
            A, B, X, Y,
            LeftShoulder, RightShoulder,
            Start, Back, Guide,
            LeftThumb, RightThumb,
            DPadUp, DPadDown, DPadLeft, DPadRight
        };

        // Default Constructor: Classic NES Style Mapping
        public MappingProfile()
        {
            // Default Mapping (Horizontal Wiimote style)
            // 2 -> A
            A.WiimoteButton = ButtonState.Two;
            // 1 -> B
            B.WiimoteButton = ButtonState.One;
            // A -> X
            X.WiimoteButton = ButtonState.A;
            // B -> Y (Trigger)
            Y.WiimoteButton = ButtonState.B;
            
            // D-Pad matches D-Pad (rotated)
            // FIXED: Inverted mappings based on user feedback
            DPadUp.WiimoteButton = ButtonState.DPadRight;    
            DPadDown.WiimoteButton = ButtonState.DPadLeft; 
            DPadLeft.WiimoteButton = ButtonState.DPadUp;  
            DPadRight.WiimoteButton = ButtonState.DPadDown;
            
            // Plus -> Start
            Start.WiimoteButton = ButtonState.Plus;
            // Minus -> Back
            Back.WiimoteButton = ButtonState.Minus;
            // Home -> Guide
            Guide.WiimoteButton = ButtonState.Home;
        }
    }
}