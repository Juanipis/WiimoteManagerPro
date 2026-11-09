using CommunityToolkit.Mvvm.ComponentModel;
using WiimoteManager.Models;

namespace WiimoteManager.Models
{
    /// <summary>
    /// Accelerometer mapping configuration for racing games and motion controls
    /// </summary>
    public class AccelerometerMapping
    {
        public bool TiltSteeringEnabled { get; set; } = false;
        public string TiltAxis { get; set; } = "Auto (X+Y for Joystick)";
        public float Sensitivity { get; set; } = 2.2f;
        public float DeadZone { get; set; } = 0.05f;
        public bool InvertAxis { get; set; } = false;
        
        // Which Xbox control to map to
        public string TargetControl { get; set; } = "LeftStick (Steering + Pitch)";
    }

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
        public const int CurrentVersion = 2; // Version tracking for migrations
        
        // Basic Info
        public string Name { get; set; } = "Default";
        public int Version { get; set; } = CurrentVersion;
        
        // Metadata
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public List<string> AssociatedGames { get; set; } = new(); // Process names or game titles
        public string Author { get; set; } = "User";
        public string IconEmoji { get; set; } = "🎮"; // Visual identifier
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public DateTime LastUsedAt { get; set; } = DateTime.MinValue;
        
        // Usage Statistics
        public int UsageCount { get; set; } = 0;
        public bool IsFavorite { get; set; } = false;
        
        // Accelerometer Settings (for racing games)
        public bool UseAccelerometer { get; set; } = false;
        public AccelerometerMapping AccelMapping { get; set; } = new();

        // Standard Buttons
        public ControlMapping A { get; set; } = new() { TargetName = "A" };
        public ControlMapping B { get; set; } = new() { TargetName = "B" };
        public ControlMapping X { get; set; } = new() { TargetName = "X" };
        public ControlMapping Y { get; set; } = new() { TargetName = "Y" };
        
        // Bumpers
        public ControlMapping LeftShoulder { get; set; } = new() { TargetName = "LB" };
        public ControlMapping RightShoulder { get; set; } = new() { TargetName = "RB" };
        public ControlMapping LeftTrigger { get; set; } = new() { TargetName = "LT" };
        public ControlMapping RightTrigger { get; set; } = new() { TargetName = "RT" };
        
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
            LeftShoulder, RightShoulder, LeftTrigger, RightTrigger,
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
        
        /// <summary>
        /// Validates the profile for consistency and completeness
        /// </summary>
        public bool IsValid(out List<string> errors)
        {
            errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Profile name cannot be empty");
            
            if (Version > CurrentVersion)
                errors.Add($"Profile version {Version} is newer than supported version {CurrentVersion}");
            
            if (UseAccelerometer && AccelMapping == null)
                errors.Add("Accelerometer enabled but mapping is null");
            
            if (UseAccelerometer && AccelMapping != null)
            {
                if (AccelMapping.Sensitivity <= 0)
                    errors.Add("Accelerometer sensitivity must be positive");
                
                if (AccelMapping.DeadZone < 0 || AccelMapping.DeadZone > 1)
                    errors.Add("Accelerometer dead zone must be between 0 and 1");
            }
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// Creates a deep copy of this profile
        /// </summary>
        public MappingProfile Clone()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this);
            var clone = System.Text.Json.JsonSerializer.Deserialize<MappingProfile>(json);
            return clone ?? new MappingProfile();
        }
        
        /// <summary>
        /// Updates usage statistics
        /// </summary>
        public void RecordUsage()
        {
            LastUsedAt = DateTime.Now;
            UsageCount++;
        }
    }
}
