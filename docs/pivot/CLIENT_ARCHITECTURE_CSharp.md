# Client Architecture: C# 3D Engine with HTML/CSS/JS Rendering

## Overview

The client uses a C# 3D engine (native development) with HTML/CSS/JavaScript rendered as textures on 3D objects. This allows for rich, interactive UI elements embedded directly in the 3D world while maintaining native C# performance and development workflow.

## Architecture Decision

### Why C# Native Client?

**Advantages:**
- **Native Development**: Both developers are C# native, faster development
- **Performance**: Native code, better performance than web-based
- **Tooling**: Full .NET ecosystem, better debugging
- **Consistency**: Server and client in same language
- **Rich 3D**: Full-featured 3D engine capabilities
- **Platform Support**: Can target Windows, Linux, macOS

**Trade-offs:**
- ❌ Requires installation (not zero-install like web)
- ❌ Platform-specific builds needed
- ✅ But: Better performance, richer features, familiar tools

### HTML/CSS/JS as Textures

**Concept:**
- Render HTML/CSS/JavaScript to texture
- Apply texture to 3D objects (consoles, screens, displays)
- Interactive elements (clickable, hoverable)
- Rich text formatting, styling, animations

**Use Cases:**
- Ship console displays
- Station information screens
- Interactive terminals
- UI overlays on objects
- Rich text displays
- Dynamic content (updates from server)

## Technology Stack

### 3D Engine Options

**Option 1: MonoGame** ⭐ **Recommended**
- **Pros**: Pure C#, cross-platform, lightweight, well-documented, open source
- **Cons**: Lower-level, more manual work, no built-in editor
- **Best for**: Full control, custom rendering, C# native development
- **Platforms**: Windows, Linux, macOS, iOS, Android, consoles

**Option 2: Godot (C#)**
- **Pros**: Open source, good C# support, cross-platform, built-in editor
- **Cons**: Less mature C# support, GDScript-first ecosystem
- **Best for**: Open source preference, visual editor needed
- **Platforms**: Windows, Linux, macOS, iOS, Android, web

**Option 3: Custom Engine (SharpDX/Vulkan/OpenGL)**
- **Pros**: Full control, minimal dependencies, exactly what you need
- **Cons**: Significant development time, no built-in features
- **Best for**: Specific requirements, long-term project, learning experience
- **Platforms**: Depends on graphics API chosen

**Option 4: Stride (formerly Xenko)**
- **Pros**: C# native, cross-platform, good tooling
- **Cons**: Smaller community, less documentation
- **Best for**: C# preference with some built-in features
- **Platforms**: Windows, Linux, macOS, iOS, Android

**Recommendation: MonoGame**
- Pure C#, lightweight, full control
- Well-established, good documentation
- Active community
- Perfect for C# native developers

### HTML Rendering Options

**Option 1: CefSharp (Chromium Embedded Framework)**
- **Pros**: Full Chromium, excellent HTML/CSS/JS support, interactive
- **Cons**: Large dependency (~100MB), Chromium license
- **Best for**: Full web compatibility, complex HTML

**Option 2: WebView2 (Microsoft Edge WebView)**
- **Pros**: Modern, maintained by Microsoft, good performance
- **Cons**: Windows-focused (though cross-platform support exists)
- **Best for**: Windows-first, modern web standards

**Option 3: AngleSharp + SkiaSharp**
- **Pros**: Lightweight, pure .NET, no browser dependency
- **Cons**: Limited JavaScript support, manual rendering
- **Best for**: Simple HTML/CSS, no complex JS

**Option 4: Avalonia WebView**
- **Pros**: Cross-platform, .NET native
- **Cons**: Less mature, smaller community
- **Best for**: Avalonia UI integration

**Recommendation: CefSharp or WebView2**
- CefSharp: Best compatibility, full web features
- WebView2: Modern, good performance, Microsoft-backed

## Architecture Design

### Component Structure

```
Client Application (C#)
├── 3D Engine (MonoGame)
│   ├── Scene Rendering
│   ├── Model Loading (STL)
│   ├── Physics Simulation
│   └── Camera/Controls
├── HTML Renderer (CefSharp/WebView2)
│   ├── HTML/CSS/JS Engine
│   ├── Texture Rendering
│   └── Input Handling
├── Network Layer
│   ├── WebSocket Client
│   └── HTTP Client
└── Game Logic
    ├── Scene Management
    ├── Object Management
    └── UI Management
```

### HTML Texture Rendering

**Process:**
1. Create off-screen browser instance (CefSharp/WebView2)
2. Load HTML/CSS/JavaScript content
3. Render browser view to texture (bitmap)
4. Apply texture to 3D object
5. Handle input (mouse clicks, keyboard) by projecting to 3D surface
6. Update texture when content changes

**Implementation:**
```csharp
public class HtmlTextureRenderer
{
    private ChromiumWebBrowser _browser;
    private Texture2D _texture;
    private int _width;
    private int _height;
    
    public HtmlTextureRenderer(int width, int height)
    {
        _width = width;
        _height = height;
        
        // Create off-screen browser
        var settings = new CefSettings();
        Cef.Initialize(settings);
        
        _browser = new ChromiumWebBrowser("about:blank");
        _browser.Size = new Size(width, height);
        
        // Create texture
        _texture = new Texture2D(width, height);
    }
    
    public void LoadHtml(string html, string css = null, string js = null)
    {
        string fullHtml = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>{css ?? ""}</style>
            </head>
            <body>
                {html}
                <script>{js ?? ""}</script>
            </body>
            </html>
        ";
        
        _browser.LoadHtml(fullHtml);
    }
    
    public Texture2D GetTexture()
    {
        // Render browser to bitmap
        Bitmap bitmap = _browser.Bitmap;
        
        // Convert bitmap to texture
        // (implementation depends on 3D engine)
        return ConvertBitmapToTexture(bitmap);
    }
    
    public void Update()
    {
        // Update texture from browser
        if (_browser.IsLoading) return;
        
        _texture = GetTexture();
    }
    
    public bool HandleInput(Vector2 screenPosition, InputType input)
    {
        // Project 3D click to 2D browser coordinates
        Vector2 browserPos = ProjectToBrowser(screenPosition);
        
        // Send input to browser
        _browser.GetBrowser().GetHost().SendMouseClickEvent(
            (int)browserPos.X, (int)browserPos.Y,
            MouseButtonType.Left, false, 1, CefEventFlags.None
        );
        
        return true;
    }
}
```

### 3D Object with HTML Texture

```csharp
public class HtmlDisplayObject
{
    private GameObject _gameObject;
    private HtmlTextureRenderer _renderer;
    private Mesh _mesh;
    private Material _material;
    
    public HtmlDisplayObject(GameObject gameObject, string htmlContent)
    {
        _gameObject = gameObject;
        
        // Create HTML renderer
        _renderer = new HtmlTextureRenderer(1024, 768);
        _renderer.LoadHtml(htmlContent);
        
        // Create mesh (plane for display)
        _mesh = CreatePlaneMesh(1.0f, 1.0f);
        
        // Create material with HTML texture
        _material = new Material();
        _material.DiffuseTexture = _renderer.GetTexture();
    }
    
    public void Update()
    {
        // Update HTML texture
        _renderer.Update();
        _material.DiffuseTexture = _renderer.GetTexture();
        
        // Update 3D transform from GameObject
        Transform.Position = _gameObject.Position;
        Transform.Rotation = _gameObject.Rotation;
    }
    
    public bool HandleClick(Vector3 worldPosition, Ray ray)
    {
        // Check if ray intersects display
        if (RayIntersectsMesh(ray, _mesh))
        {
            // Project to 2D screen space
            Vector2 screenPos = ProjectToScreen(worldPosition);
            
            // Forward to HTML renderer
            return _renderer.HandleInput(screenPos, InputType.Click);
        }
        
        return false;
    }
}
```

## Use Cases

### Ship Console Displays

**Example:**
```csharp
// Console object with HTML display
var console = new HtmlDisplayObject(consoleGameObject, @"
    <div class='console'>
        <h2>Science Console</h2>
        <div id='sensor-data'>
            <p>Scanning...</p>
        </div>
        <button onclick='scan()'>Scan</button>
    </div>
    <style>
        .console { background: #1a1a2e; color: #0f3460; }
        button { background: #00ff00; }
    </style>
    <script>
        function scan() {
            // Send command to server
            sendCommand('scan');
        }
    </script>
");
```

**Benefits:**
- Rich formatting (HTML/CSS)
- Interactive buttons
- Dynamic content updates
- Styled to match game aesthetic

### Station Information Screens

**Example:**
```csharp
var infoScreen = new HtmlDisplayObject(stationScreen, @"
    <div class='info-screen'>
        <h1>Welcome to Station Alpha</h1>
        <div class='services'>
            <ul>
                <li>Docking: Available</li>
                <li>Refueling: Available</li>
                <li>Repairs: Available</li>
            </ul>
        </div>
        <div class='market'>
            <h2>Market Prices</h2>
            <table id='prices'></table>
        </div>
    </div>
");
```

### Interactive Terminals

**Example:**
```csharp
var terminal = new HtmlDisplayObject(terminalObject, @"
    <div class='terminal'>
        <div id='output'></div>
        <input type='text' id='input' onkeypress='handleKeyPress(event)' />
    </div>
    <script>
        function handleKeyPress(event) {
            if (event.key === 'Enter') {
                sendCommand(document.getElementById('input').value);
            }
        }
    </script>
");
```

## Integration with Server

### Content Updates

**Server sends HTML updates:**
```json
{
  "type": "htmlUpdate",
  "objectId": "console-1",
  "html": "<div>New content</div>",
  "css": "body { color: red; }",
  "js": "updateData();"
}
```

**Client updates display:**
```csharp
void HandleHtmlUpdate(HtmlUpdateMessage message)
{
    var display = GetHtmlDisplay(message.objectId);
    if (display != null)
    {
        display.Renderer.LoadHtml(message.html, message.css, message.js);
    }
}
```

### Interactive Commands

**Client sends commands from HTML:**
```javascript
// In HTML/JS
function sendCommand(command) {
    // Send via WebSocket
    websocket.send(JSON.stringify({
        type: "action",
        verb: command,
        target: "console-1"
    }));
}
```

**Server processes:**
- Normal verb/function execution
- Returns result
- Updates HTML content if needed

## Performance Considerations

### Texture Updates

**Optimization:**
- Only update textures when content changes
- Cache rendered textures
- Use lower resolution for distant objects
- LOD (Level of Detail) for HTML displays

**Update Frequency:**
```csharp
void Update()
{
    // Update HTML textures (throttled)
    if (TimeSinceLastUpdate > 0.1f) // 10 FPS for HTML updates
    {
        UpdateHtmlTextures();
    }
    
    // Update 3D rendering (60 FPS)
    Render3D();
}
```

### Memory Management

**Browser Instances:**
- Reuse browser instances when possible
- Dispose unused instances
- Limit concurrent HTML displays
- Pool browser instances

### Rendering Performance

**Texture Size:**
- Use appropriate resolution (1024x768 for close, 512x384 for distant)
- Scale based on distance
- Compress textures
- Use texture atlases if possible

## Development Workflow

### HTML/CSS/JS Development

**Options:**
1. **Inline in C#**: Define HTML as strings in code
2. **External Files**: Load HTML from files
3. **Server-Generated**: Server generates HTML, client renders
4. **Template System**: Use templates, fill with data

**Recommended: Hybrid**
- Templates defined in code/files
- Server provides data
- Client renders with data

### Testing

**HTML Testing:**
- Test HTML/CSS/JS in browser first
- Then test in C# renderer
- Verify interactivity
- Check performance

**3D Integration Testing:**
- Test texture rendering
- Test input handling
- Test performance
- Test multiple displays

## Platform Considerations

### Windows

**Best Support:**
- WebView2 (native)
- CefSharp (well-supported)
- Full .NET ecosystem

### Linux

**Options:**
- CefSharp (works, but larger)
- WebView2 (limited support)
- AngleSharp + SkiaSharp (pure .NET)

### macOS

**Options:**
- CefSharp (works)
- WebView2 (limited)
- Native WebKit (via interop)

## Comparison: Web Client vs C# Client

### Web Client (Original Plan)

**Pros:**
- Zero installation
- Cross-platform (browser)
- Easy deployment
- No compilation needed

**Cons:**
- WebGL limitations
- JavaScript development
- Less native performance
- Browser compatibility

### C# Client (New Plan)

**Pros:**
- Native C# development
- Better performance
- Full 3D engine features
- Rich tooling
- HTML/CSS/JS for UI flexibility

**Cons:**
- Requires installation
- Platform-specific builds
- Larger download
- More complex deployment

## Recommended Architecture

### Phase 1: Core 3D Engine

- Choose engine (MonoGame recommended)
- Implement STL loading
- Basic 3D rendering
- Camera/controls
- Network connection

### Phase 2: HTML Rendering

- Integrate HTML renderer (CefSharp/WebView2)
- Implement texture rendering
- Basic HTML display on objects
- Input handling

### Phase 3: Integration

- Connect HTML displays to server
- Dynamic content updates
- Interactive elements
- Multiple displays

### Phase 4: Polish

- Performance optimization
- UI/UX improvements
- Advanced features
- Platform-specific optimizations

## Implementation Notes

### MonoGame Example

```csharp
// MonoGame with CefSharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private HtmlTextureRenderer _htmlRenderer;
    private BasicEffect _basicEffect;
    private VertexBuffer _vertexBuffer;
    
    protected override void Initialize()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        // Initialize HTML renderer
        _htmlRenderer = new HtmlTextureRenderer(1024, 768);
        
        base.Initialize();
    }
    
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // Load HTML content
        _htmlRenderer.LoadHtml("<h1>Hello World</h1>");
        
        // Setup 3D rendering
        _basicEffect = new BasicEffect(GraphicsDevice);
        _basicEffect.VertexColorEnabled = true;
        
        // Create plane for HTML display
        CreateDisplayPlane();
    }
    
    protected override void Update(GameTime gameTime)
    {
        // Update HTML renderer
        _htmlRenderer.Update();
        
        base.Update(gameTime);
    }
    
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        
        // Render 3D scene with HTML textures
        _basicEffect.Texture = _htmlRenderer.GetTexture();
        _basicEffect.TextureEnabled = true;
        
        GraphicsDevice.SetVertexBuffer(_vertexBuffer);
        
        foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
        }
        
        base.Draw(gameTime);
    }
    
    private void CreateDisplayPlane()
    {
        // Create a quad for displaying HTML texture
        var vertices = new VertexPositionTexture[]
        {
            new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0))
        };
        
        _vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);
    }
}
```

## Conclusion

A C# native client with HTML/CSS/JS texture rendering offers:
- **Familiar Development**: C# for both server and client
- **Rich Features**: Full 3D engine capabilities
- **Flexible UI**: HTML/CSS/JS for rich, interactive displays
- **Performance**: Native code performance
- **Best of Both**: 3D engine power + web UI flexibility

This approach is definitely doable and may be better suited for your team's skills and project requirements.
