// Dynamic HTML login banner using LESS CSS functionality
// Define theme variables for consistent styling
var theme = new Dictionary<string, string>
{
    ["primary"] = "#00ff88",
    ["secondary"] = "#88ddff",
    ["accent"] = "#ffaa00",
    ["text-light"] = "#cccccc",
    ["text-white"] = "#ffffff",
    ["text-dim"] = "#888888",
    ["background-dark"] = "#000000",
    ["glow-shadow"] = "0 0 30px #00ff88",
    ["border-primary"] = "2px solid #00ff88",
    ["border-accent"] = "1px solid #ffaa00",
    ["spacing-small"] = "10px",
    ["spacing-medium"] = "20px",
    ["spacing-large"] = "30px",
    ["spacing-xlarge"] = "40px",
    ["border-radius"] = "15px",
    ["border-radius-pill"] = "25px"
};

// Create the main container with cyberpunk styling
var container = Html.CyberpunkContainer()
    .WithLessStyle("margin:0; padding:@spacing-medium; min-height:100vh; display:flex; flex-direction:column; align-items:center; justify-content:center;", theme);

// Create the inner content box
var contentBox = Html.Div()
    .WithLessStyle("max-width:800px; text-align:center; padding:@spacing-xlarge; border:@border-primary; border-radius:@border-radius; background:@background-dark; box-shadow:@glow-shadow;", theme);

// Add title
var title = Html.GlowTitle("CSMOO");

// Add subtitle
var subtitle = Html.Div("Multi-User Object Oriented Server")
    .WithLessStyle("font-size:1.2rem; color:@secondary; margin-bottom:@spacing-large; letter-spacing:2px;", theme);

// Add version badge
var version = Html.Div("Version 1.0.0 (Dynamic!)")
    .WithLessStyle("font-size:1rem; color:@accent; margin-bottom:@spacing-xlarge; padding:@spacing-small @spacing-medium; border:@border-accent; border-radius:@border-radius; display:inline-block;", theme);

// Add welcome message
var welcome = Html.Div("*** Welcome to CSMOO! ***")
    .WithLessStyle("font-size:1.3rem; margin-bottom:@spacing-large; color:@text-white;", theme);

// Add instructions container
var instructions = Html.Div()
    .WithLessStyle("font-size:1.1rem; line-height:1.8; color:@text-light; margin-bottom:@spacing-medium;", theme);

// Add instruction paragraphs
var intro = Html.P("*** Enter the world of collaborative object-oriented programming and exploration! ***");
var loginInstr = Html.P()
  .AppendChild(Html.Element("span", "> To connect: "))
  .AppendChild(Html.Command("login <username> <password>"));
var createInstr = Html.P()
  .AppendChild(Html.Element("span", "> New player: "))
  .AppendChild(Html.Command("create player <username> <password>"));

// Add footer
var footer = Html.Div(">> Powered by C# and imagination • Built with ❤️ for MUD enthusiasts! <<")
    .WithLessStyle("margin-top:@spacing-xlarge; font-size:0.9rem; color:@text-dim; font-style:italic;", theme);

// Assemble the complete structure
instructions.AppendChildren(intro, loginInstr, createInstr);
contentBox.AppendChildren(title, subtitle, version, welcome, instructions, footer);
container.AppendChild(contentBox);

// Return as single line for MUD client compatibility
return container.ToSingleLine();
