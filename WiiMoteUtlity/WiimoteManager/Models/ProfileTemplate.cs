using WiimoteManager.Models;

namespace WiimoteManager.Models;

/// <summary>
/// Pre-built profile templates for popular game genres
/// </summary>
public abstract class ProfileTemplate
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string IconEmoji { get; }
    public abstract List<string> Tags { get; }
    public abstract List<string> SuggestedGames { get; }
    
    public abstract MappingProfile CreateProfile();
}

/// <summary>
/// Racing game profile with accelerometer steering
/// </summary>
public class RacingGameTemplate : ProfileTemplate
{
    public override string Name => "Racing Game (Tilt Steering)";
    public override string Description => "Motion controls for racing games. Tilt to steer, buttons for gas/brake.";
    public override string IconEmoji => "🏎️";
    public override List<string> Tags => new() { "Racing", "Motion", "Accelerometer" };
    public override List<string> SuggestedGames => new() 
    { 
        "Need for Speed", 
        "Forza Horizon", 
        "Mario Kart",
        "TrackMania",
        "Rocket League"
    };
    
    public override MappingProfile CreateProfile()
    {
        var profile = new MappingProfile
        {
            Name = Name,
            Description = Description,
            IconEmoji = IconEmoji,
            Tags = new List<string>(Tags),
            AssociatedGames = new List<string>(SuggestedGames),
            Author = "System",
            UseAccelerometer = true,
            AccelMapping = new AccelerometerMapping
            {
                TiltSteeringEnabled = true,
                TiltAxis = "Auto (X+Y for Joystick)", // Dual Axis
                Sensitivity = 2.2f,
                DeadZone = 0.05f,
                InvertAxis = false,
                TargetControl = "LeftStick (Steering + Pitch)"
            }
        };
        
        // Button mappings for racing
        profile.A.WiimoteButton = ButtonState.Two; // Accelerate
        profile.B.WiimoteButton = ButtonState.One; // Brake/Reverse
        profile.X.WiimoteButton = ButtonState.A; // Handbrake
        profile.Y.WiimoteButton = ButtonState.B; // Nitro/Boost
        
        profile.LeftShoulder.WiimoteButton = ButtonState.DPadLeft; // Look Left
        profile.RightShoulder.WiimoteButton = ButtonState.DPadRight; // Look Right
        
        profile.DPadUp.WiimoteButton = ButtonState.DPadUp; // Camera Up
        profile.DPadDown.WiimoteButton = ButtonState.DPadDown; // Camera Down
        
        profile.Start.WiimoteButton = ButtonState.Plus;
        profile.Back.WiimoteButton = ButtonState.Minus; // Change View
        profile.Guide.WiimoteButton = ButtonState.Home;
        
        return profile;
    }
}

/// <summary>
/// Classic platformer profile (Mario, Sonic style)
/// </summary>
public class PlatformerTemplate : ProfileTemplate
{
    public override string Name => "Platformer (Mario Style)";
    public override string Description => "Optimized for 2D platformers. Classic NES-style horizontal grip.";
    public override string IconEmoji => "🍄";
    public override List<string> Tags => new() { "Platformer", "2D", "Classic" };
    public override List<string> SuggestedGames => new() 
    { 
        "Super Mario", 
        "Sonic", 
        "Celeste",
        "Hollow Knight",
        "Cuphead"
    };
    
    public override MappingProfile CreateProfile()
    {
        var profile = new MappingProfile
        {
            Name = Name,
            Description = Description,
            IconEmoji = IconEmoji,
            Tags = new List<string>(Tags),
            AssociatedGames = new List<string>(SuggestedGames),
            Author = "System"
        };
        
        // Classic horizontal Wiimote mapping
        profile.A.WiimoteButton = ButtonState.Two; // Jump
        profile.B.WiimoteButton = ButtonState.One; // Run/Action
        profile.X.WiimoteButton = ButtonState.A; // Crouch/Slide
        profile.Y.WiimoteButton = ButtonState.B; // Special Move
        
        // D-Pad for movement (rotated for horizontal grip)
        profile.DPadUp.WiimoteButton = ButtonState.DPadRight;    
        profile.DPadDown.WiimoteButton = ButtonState.DPadLeft; 
        profile.DPadLeft.WiimoteButton = ButtonState.DPadUp;  
        profile.DPadRight.WiimoteButton = ButtonState.DPadDown;
        
        profile.Start.WiimoteButton = ButtonState.Plus;
        profile.Back.WiimoteButton = ButtonState.Minus;
        profile.Guide.WiimoteButton = ButtonState.Home;
        
        return profile;
    }
}

/// <summary>
/// Fighting game profile
/// </summary>
public class FightingGameTemplate : ProfileTemplate
{
    public override string Name => "Fighting Game";
    public override string Description => "Button layout for fighting games with quick access to all buttons.";
    public override string IconEmoji => "🥊";
    public override List<string> Tags => new() { "Fighting", "Arcade" };
    public override List<string> SuggestedGames => new() 
    { 
        "Street Fighter", 
        "Mortal Kombat", 
        "Tekken",
        "Guilty Gear",
        "Super Smash Bros"
    };
    
    public override MappingProfile CreateProfile()
    {
        var profile = new MappingProfile
        {
            Name = Name,
            Description = Description,
            IconEmoji = IconEmoji,
            Tags = new List<string>(Tags),
            AssociatedGames = new List<string>(SuggestedGames),
            Author = "System"
        };
        
        // All buttons easily accessible
        profile.A.WiimoteButton = ButtonState.A; // Light Punch
        profile.B.WiimoteButton = ButtonState.Two; // Heavy Punch
        profile.X.WiimoteButton = ButtonState.B; // Light Kick
        profile.Y.WiimoteButton = ButtonState.One; // Heavy Kick
        
        profile.LeftShoulder.WiimoteButton = ButtonState.Minus; // Block
        profile.RightShoulder.WiimoteButton = ButtonState.Plus; // Throw
        
        // D-Pad for movement
        profile.DPadUp.WiimoteButton = ButtonState.DPadUp;
        profile.DPadDown.WiimoteButton = ButtonState.DPadDown;
        profile.DPadLeft.WiimoteButton = ButtonState.DPadLeft;
        profile.DPadRight.WiimoteButton = ButtonState.DPadRight;
        
        profile.Guide.WiimoteButton = ButtonState.Home;
        
        return profile;
    }
}

/// <summary>
/// FPS/Shooter game profile
/// </summary>
public class ShooterTemplate : ProfileTemplate
{
    public override string Name => "FPS/Shooter";
    public override string Description => "First-person shooter layout with aim and shoot controls.";
    public override string IconEmoji => "🔫";
    public override List<string> Tags => new() { "FPS", "Shooter", "Action" };
    public override List<string> SuggestedGames => new() 
    { 
        "Call of Duty", 
        "Halo", 
        "Counter-Strike",
        "Doom",
        "Battlefield"
    };
    
    public override MappingProfile CreateProfile()
    {
        var profile = new MappingProfile
        {
            Name = Name,
            Description = Description,
            IconEmoji = IconEmoji,
            Tags = new List<string>(Tags),
            AssociatedGames = new List<string>(SuggestedGames),
            Author = "System"
        };
        
        // Shooter controls
        profile.A.WiimoteButton = ButtonState.A; // Jump
        profile.B.WiimoteButton = ButtonState.B; // Shoot/Fire
        profile.X.WiimoteButton = ButtonState.One; // Reload
        profile.Y.WiimoteButton = ButtonState.Two; // Switch Weapon
        
        profile.LeftShoulder.WiimoteButton = ButtonState.Minus; // Grenade
        profile.RightShoulder.WiimoteButton = ButtonState.Plus; // Aim Down Sights
        
        profile.DPadUp.WiimoteButton = ButtonState.DPadUp; // Forward
        profile.DPadDown.WiimoteButton = ButtonState.DPadDown; // Back
        profile.DPadLeft.WiimoteButton = ButtonState.DPadLeft; // Strafe Left
        profile.DPadRight.WiimoteButton = ButtonState.DPadRight; // Strafe Right
        
        profile.LeftThumb.WiimoteButton = null; // Sprint (hold)
        profile.RightThumb.WiimoteButton = null; // Crouch
        
        profile.Guide.WiimoteButton = ButtonState.Home;
        
        return profile;
    }
}

/// <summary>
/// Sports game profile
/// </summary>
public class SportsTemplate : ProfileTemplate
{
    public override string Name => "Sports Game";
    public override string Description => "General sports game controls (FIFA, NBA, etc.).";
    public override string IconEmoji => "⚽";
    public override List<string> Tags => new() { "Sports", "FIFA", "NBA" };
    public override List<string> SuggestedGames => new() 
    { 
        "FIFA", 
        "NBA 2K", 
        "Madden NFL",
        "Rocket League",
        "WWE 2K"
    };
    
    public override MappingProfile CreateProfile()
    {
        var profile = new MappingProfile
        {
            Name = Name,
            Description = Description,
            IconEmoji = IconEmoji,
            Tags = new List<string>(Tags),
            AssociatedGames = new List<string>(SuggestedGames),
            Author = "System"
        };
        
        profile.A.WiimoteButton = ButtonState.A; // Pass/Select
        profile.B.WiimoteButton = ButtonState.B; // Shoot/Confirm
        profile.X.WiimoteButton = ButtonState.One; // Through Ball/Special
        profile.Y.WiimoteButton = ButtonState.Two; // Lob/Switch
        
        profile.LeftShoulder.WiimoteButton = ButtonState.Minus; // Sprint
        profile.RightShoulder.WiimoteButton = ButtonState.Plus; // Special Move
        
        profile.DPadUp.WiimoteButton = ButtonState.DPadUp;
        profile.DPadDown.WiimoteButton = ButtonState.DPadDown;
        profile.DPadLeft.WiimoteButton = ButtonState.DPadLeft;
        profile.DPadRight.WiimoteButton = ButtonState.DPadRight;
        
        profile.Start.WiimoteButton = ButtonState.Plus;
        profile.Back.WiimoteButton = ButtonState.Minus;
        profile.Guide.WiimoteButton = ButtonState.Home;
        
        return profile;
    }
}

/// <summary>
/// Party Game profile (Ultimate Chicken Horse, etc.)
/// </summary>
public class PartyGameTemplate : ProfileTemplate
{
    public override string Name => "Party Game (Ultimate Chicken Horse)";
    public override string Description => "Optimized for party games. Horizontal grip with motion support.";
    public override string IconEmoji => "🐔";
    public override List<string> Tags => new() { "Party", "Arcade", "Platformer" };
    public override List<string> SuggestedGames => new() 
    { 
        "Ultimate Chicken Horse", 
        "Mario Party", 
        "Jackbox Party Pack",
        "Overcooked",
        "Move or Die"
    };
    
    public override MappingProfile CreateProfile()
    {
        var profile = new MappingProfile
        {
            Name = Name,
            Description = Description,
            IconEmoji = IconEmoji,
            Tags = new List<string>(Tags),
            AssociatedGames = new List<string>(SuggestedGames),
            Author = "System"
        };
        
        // Horizontal Grip Mapping
        profile.A.WiimoteButton = ButtonState.Two; // Jump (Standard NES style)
        profile.B.WiimoteButton = ButtonState.One; // Action/Sprint
        profile.X.WiimoteButton = ButtonState.B; // Secondary Action (Trigger)
        profile.Y.WiimoteButton = ButtonState.A; // Taunt/Interact
        
        // D-Pad for movement (Rotated 90 degrees for horizontal grip)
        profile.DPadUp.WiimoteButton = ButtonState.DPadRight;
        profile.DPadDown.WiimoteButton = ButtonState.DPadLeft;
        profile.DPadLeft.WiimoteButton = ButtonState.DPadUp;
        profile.DPadRight.WiimoteButton = ButtonState.DPadDown;
        
        // Menu Controls
        profile.Start.WiimoteButton = ButtonState.Plus; // Pause/Menu
        profile.Back.WiimoteButton = ButtonState.Minus; // Scoreboard/Back
        profile.Guide.WiimoteButton = ButtonState.Home;
        
        // Shoulders (mapped to same as face buttons for accessibility)
        profile.LeftShoulder.WiimoteButton = ButtonState.One;
        profile.RightShoulder.WiimoteButton = ButtonState.Two;
        
        return profile;
    }
}

/// <summary>
/// Rocket League profile with dedicated button layout and tilt steering.
/// </summary>
public class RocketLeagueTemplate : ProfileTemplate
{
    public override string Name => "Rocket League (Tilt Pro)";
    public override string Description => "Rocket League focused profile: 2 accelerate, 1 jump, B boost, A brake, tilt steering.";
    public override string IconEmoji => "🚗";
    public override List<string> Tags => new() { "Rocket League", "Racing", "Tilt", "Competitive" };
    public override List<string> SuggestedGames => new() { "Rocket League" };

    public override MappingProfile CreateProfile()
    {
        var profile = new MappingProfile
        {
            Name = Name,
            Description = Description,
            IconEmoji = IconEmoji,
            Tags = new List<string>(Tags),
            AssociatedGames = new List<string>(SuggestedGames),
            Author = "System",
            UseAccelerometer = true,
            AccelMapping = new AccelerometerMapping
            {
                TiltSteeringEnabled = true,
                TiltAxis = "Auto (X+Y for Joystick)",
                Sensitivity = 1.4f,
                DeadZone = 0.06f,
                InvertAxis = false,
                TargetControl = "LeftStick (Steering + Pitch)"
            }
        };

        // Requested layout:
        // 2 -> Accelerate, 1 -> Jump, B -> Nitro, A -> Brake
        profile.RightTrigger.WiimoteButton = ButtonState.Two; // Accelerate
        profile.A.WiimoteButton = ButtonState.One;             // Jump
        profile.B.WiimoteButton = ButtonState.B;               // Nitro/Boost
        profile.LeftTrigger.WiimoteButton = ButtonState.A;     // Brake/Reverse
        profile.X.WiimoteButton = null;
        profile.Y.WiimoteButton = null;
        profile.LeftShoulder.WiimoteButton = null;
        profile.RightShoulder.WiimoteButton = null;

        profile.Start.WiimoteButton = ButtonState.Plus;
        profile.Back.WiimoteButton = ButtonState.Minus;
        profile.Guide.WiimoteButton = ButtonState.Home;

        return profile;
    }
}

/// <summary>
/// Template manager for easy access to all templates
/// </summary>
public static class ProfileTemplates
{
    private static readonly List<ProfileTemplate> _templates = new()
    {
        new RacingGameTemplate(),
        new PlatformerTemplate(),
        new FightingGameTemplate(),
        new ShooterTemplate(),
        new SportsTemplate(),
        new PartyGameTemplate(),
        new RocketLeagueTemplate()
    };
    
    public static List<ProfileTemplate> GetAllTemplates() => _templates;
    
    public static ProfileTemplate? GetTemplateByName(string name)
    {
        return _templates.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
