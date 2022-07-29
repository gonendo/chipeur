using System;
using System.IO;
using System.Numerics;
using System.Text;
#if Windows
using System.Windows.Forms;
#endif
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImGuiNET;
#if Linux
using NativeFileDialogSharp;
#endif
using chipeur.cpu;
using chipeur.input;
using chipeur.sound;
using chipeur.graphics;

namespace chipeur.gui
{
    class Gui{
        private const int WINDOW_WIDTH = 640;
        private const int WINDOW_HEIGHT = 480;
        private static GraphicsDevice _graphicsDevice;
        private static Sdl2Window _window;
        private static CommandList _commandList;
        private static ImGuiRenderer _imguiRenderer;
        private static Image<Rgba32> _image;
        private static DeviceBuffer _vertexBuffer;
        private static DeviceBuffer _indexBuffer;
        private static Shader[] _shaders;
        private static Pipeline _pipeline;
        private static ResourceSet _resourceSet;
        private static ResourceLayout _resourceLayout;
        private static IntPtr _chipeurImage;

        public static event Action Quit;
        public static event Action<Key> KeyDown;
        public static event Action<Key> KeyUp;
        public static event Action<int> ChangeSpeed;
        public static event Action<int> ChangeKeyboardLayout;
        public static event Action<string> LoadRom;
        public static event Action<int> ChangeProfile;

        private static bool _imguiLoadRom;
        private static bool _imguiQuit;
        private static bool _imguiAzerty;
        private static bool _imguiQwerty = true;
        private static bool _imguiSpeed60hz;
        private static bool _imguiSpeed500hz = true;
        private static bool _imguiSpeed1000hz;
        private static bool _imguiSoundOn = true;
        private static bool _imguiSoundOff;
        private static bool _imguiHelpAbout;
        private static bool _imguiAboutOpen;
        private static bool _imguiCompatibilityChip8 = true;
        private static bool _imguiCompatibilitySuperChip;

        private static bool _aboutWindowVisible;
        public static bool menuBarVisible;

        public Gui(){
            SDL2.SDL.SDL_DisplayMode current;
            int displayWidth = 0;
            int displayHeight = 0;
            for(int i=0; i < SDL2.SDL.SDL_GetNumVideoDisplays(); ++i){
                if(SDL2.SDL.SDL_GetCurrentDisplayMode(i, out current) == 0){
                    displayWidth = current.w;
                    displayHeight = current.h;
                    break;
                }
            }
            int winX = displayWidth > 0 ? displayWidth/2 - WINDOW_WIDTH/2 : 100;
            int winY = displayHeight > 0 ? displayHeight/2 - WINDOW_HEIGHT/2 : 100;
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(winX, winY, WINDOW_WIDTH, WINDOW_HEIGHT, WindowState.Hidden, "Chipeur - Chip8 Emulator"),
                new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out _graphicsDevice);

            _imguiRenderer = new ImGuiRenderer(
                _graphicsDevice,
                _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
                (int)_graphicsDevice.MainSwapchain.Framebuffer.Width,
                (int)_graphicsDevice.MainSwapchain.Framebuffer.Height);

            _window.Resized += () =>
            {
                _graphicsDevice.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _imguiRenderer.WindowResized(_window.Width, _window.Height);
            };
            _window.Closing += () =>
            {
                Quit.Invoke();
            };
            _window.KeyDown += (KeyEvent e) =>
            {
                KeyDown.Invoke(e.Key);
            };
            _window.KeyUp += (KeyEvent e) =>
            {
                KeyUp.Invoke(e.Key);
            };
            _window.MouseUp += (MouseEvent e) =>
            {
                if(!ImGui.IsAnyItemHovered()){
                    menuBarVisible = !menuBarVisible;
                }
            };

            _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();

            Configuration config = Configuration.Default.Clone();
            config.PreferContiguousImageBuffers  = true;

            Chip8.ChangeDisplayResolution += (int displayWidth, int displayHeight) =>
            {
                _image = new Image<Rgba32>(config, displayWidth, displayHeight);
            };

            CreateResources();

            var img = new ImageSharpTexture("assets/chipeur.png");
            var dimg = img.CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory);
            var viewDesc = new TextureViewDescription(dimg, PixelFormat.R8_G8_B8_A8_UNorm);
            var textureView = _graphicsDevice.ResourceFactory.CreateTextureView(viewDesc);
            _chipeurImage = _imguiRenderer.GetOrCreateImGuiBinding(_graphicsDevice.ResourceFactory, textureView);
        }

        private static void CreateResources(){
            ResourceFactory factory = _graphicsDevice.ResourceFactory;

            Vector4[] quadVerts =
            {
                new Vector4(-1, 1, 0, 0),
                new Vector4(1, 1, 1, 0),
                new Vector4(1, -1, 1, 1),
                new Vector4(-1, -1, 0, 1),
            };

            ushort[] indices = { 0, 1, 2, 0, 2, 3 };
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(4 * 16, BufferUsage.VertexBuffer));
            _indexBuffer = factory.CreateBuffer(new BufferDescription(2 * 6, BufferUsage.IndexBuffer));
            _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVerts);
            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, indices);

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

            ShaderDescription vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(VertexCode),
                "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(FragmentCode),
                "main");

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
            ShaderSetDescription shaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: _shaders);

            _resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Tex11", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Tex22", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SS", ResourceKind.Sampler, ShaderStages.Fragment)));

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { _resourceLayout },
                _graphicsDevice.SwapchainFramebuffer.OutputDescription
            );

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
        }

        private static unsafe void SubmitUI(){
            if(menuBarVisible && ImGui.BeginMainMenuBar()){
                if(ImGui.BeginMenu("File")){
                    ImGui.MenuItem("Load Rom...", null, ref _imguiLoadRom);
                    ImGui.MenuItem("Quit", null, ref _imguiQuit);
                    ImGui.EndMenu();
                }
                if(ImGui.BeginMenu("Options")){
                    if(ImGui.BeginMenu("Compatibility")){
                        ImGui.MenuItem("Chip 8 (Cosmac VIP)", null, ref _imguiCompatibilityChip8);
                        ImGui.MenuItem("SuperChip 1.1", null, ref _imguiCompatibilitySuperChip);
                        ImGui.EndMenu();
                    }
                    if(ImGui.BeginMenu("Speed")){
                        ImGui.MenuItem("60 hz", null, ref _imguiSpeed60hz);
                        ImGui.MenuItem("500 hz", null, ref _imguiSpeed500hz);
                        ImGui.MenuItem("1000 hz", null, ref _imguiSpeed1000hz);
                        ImGui.EndMenu();
                    }
                    if(ImGui.BeginMenu("Sound")){
                        ImGui.MenuItem("On", null, ref _imguiSoundOn);
                        ImGui.MenuItem("Off", null, ref _imguiSoundOff);
                        ImGui.EndMenu();
                    }
                    if(ImGui.BeginMenu("Keyboard")){
                        ImGui.MenuItem("Azerty", null, ref _imguiAzerty);
                        ImGui.MenuItem("Qwerty", null, ref _imguiQwerty);
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenu();
                }
                if(ImGui.BeginMenu("Help")){
                    ImGui.MenuItem("About", null, ref _imguiHelpAbout);
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
            if(_aboutWindowVisible && ImGui.Begin("About", ref _imguiAboutOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings)){
                ImGui.Text("-- Chipeur version "+Program.VERSION+" --\n\nChipeur is a Chip8 Emulator written by Brice Andriantafika.");
                ImGui.Image(_chipeurImage, new Vector2(133, 100));
                ImGui.End();
            }
        }

        private static void HandleUiEvents(){
            if(_imguiLoadRom){
                #if Linux
                  NativeFileDialogSharp.DialogResult result = Dialog.FileOpen(null, Directory.GetCurrentDirectory() + "/roms");
                  if(result.Path != null){
                    LoadRom.Invoke(result.Path);
                  }
                #elif Windows
                   OpenFileDialog openFileDialog = new OpenFileDialog();
                   openFileDialog.InitialDirectory = System.IO.Path.GetFullPath(Directory.GetCurrentDirectory() + "/roms");
                   if(openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK){
                     LoadRom.Invoke(openFileDialog.FileName);
                   }
                #endif
                _imguiLoadRom = false;
            }
            if(_imguiQuit){
                _imguiQuit = false;
                Quit.Invoke();
            }
            if(_imguiAzerty){
                if(Input.keyboardLayout != Input.KEYBOARD_LAYOUT_AZERTY){
                    ChangeKeyboardLayout.Invoke(Input.KEYBOARD_LAYOUT_AZERTY);
                    _imguiQwerty = false;
                }
            }
            if(_imguiQwerty){
                if(Input.keyboardLayout != Input.KEYBOARD_LAYOUT_QWERTY){
                    ChangeKeyboardLayout.Invoke(Input.KEYBOARD_LAYOUT_QWERTY);
                    _imguiAzerty = false;
                }
            }
            if(_imguiSpeed60hz){
                if(Chip8.speedInHz != 60){
                    ChangeSpeed.Invoke(60);
                    _imguiSpeed500hz = false;
                    _imguiSpeed1000hz = false;
                }
            }
            if(_imguiSpeed500hz){
                if(Chip8.speedInHz != 500){
                    ChangeSpeed.Invoke(500);
                    _imguiSpeed60hz = false;
                    _imguiSpeed1000hz = false;
                }
            }
            if(_imguiSpeed1000hz){
                if(Chip8.speedInHz != 1000){
                    ChangeSpeed.Invoke(1000);
                    _imguiSpeed60hz = false;
                    _imguiSpeed500hz = false;
                }
            }
            if(_imguiSoundOn){
                if(Sounds.mute){
                    Sounds.mute = false;
                    _imguiSoundOff = false;
                }
            }
            if(_imguiSoundOff){
                if(!Sounds.mute){
                    Sounds.mute = true;
                    _imguiSoundOn = false;
                }
            }
            if(_imguiHelpAbout){
                _imguiAboutOpen = true;
                _aboutWindowVisible = true;
                _imguiHelpAbout = false;
            }
            if(!_imguiAboutOpen){
                _aboutWindowVisible = false;
            }

            if(_imguiCompatibilityChip8){
                if(Chip8.profile != Chip8.PROFILE_CHIP8){
                    ChangeProfile.Invoke(Chip8.PROFILE_CHIP8);
                    _imguiCompatibilitySuperChip = false;
                }
                _imguiCompatibilityChip8 = false;
            }
            if(_imguiCompatibilitySuperChip){
                if(Chip8.profile != Chip8.PROFILE_SUPERCHIP){
                    ChangeProfile.Invoke(Chip8.PROFILE_SUPERCHIP);
                    _imguiCompatibilityChip8 = false;
                }
                _imguiCompatibilitySuperChip = false;
            }

            _imguiAzerty = Input.keyboardLayout == Input.KEYBOARD_LAYOUT_AZERTY;
            _imguiQwerty = Input.keyboardLayout == Input.KEYBOARD_LAYOUT_QWERTY;
            _imguiSpeed60hz = Chip8.speedInHz == 60;
            _imguiSpeed500hz = Chip8.speedInHz == 500;
            _imguiSpeed1000hz = Chip8.speedInHz == 1000;
            _imguiSoundOn = !Sounds.mute;
            _imguiSoundOff = Sounds.mute;
            _imguiCompatibilityChip8 = Chip8.profile == Chip8.PROFILE_CHIP8;
            _imguiCompatibilitySuperChip = Chip8.profile == Chip8.PROFILE_SUPERCHIP; 
        }

        private static void DrawTexture(){
            for(int i=0; i < Chip8.displayWidth; i++){
                for(int j=0; j < Chip8.displayHeight; j++){
                    _image[i, j] = new Rgba32(Graphics.pixelsBuffer[i + j*Chip8.displayWidth]);
                }
            }

            ImageSharpTexture imgSharpTexture = new ImageSharpTexture(_image);
            Texture texture = imgSharpTexture.CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory);

            TextureView textureView = _graphicsDevice.ResourceFactory.CreateTextureView(texture);
            BindableResource[] resources = new BindableResource[] {textureView, textureView, textureView, _graphicsDevice.PointSampler};
            _resourceSet = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                _resourceLayout,
                resources
            ));

            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _commandList.SetPipeline(_pipeline);
            _commandList.SetGraphicsResourceSet(0, _resourceSet);
            _commandList.DrawIndexed(6, 1, 0, 0, 0);
        }

        public void Update(){
            HandleUiEvents();

            if(_window.Exists){
                if(!_window.Visible){
                    _window.Visible = true;
                }
                InputSnapshot snapshot = _window.PumpEvents();

                if(!_window.Exists){
                    return;
                }

                _imguiRenderer.Update(1f / 60f, snapshot);

                SubmitUI();

                _commandList.Begin();
                _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
                _commandList.ClearColorTarget(0, RgbaFloat.Black);
                DrawTexture();
                _imguiRenderer.Render(_graphicsDevice, _commandList);
                _commandList.End();

                _graphicsDevice.SubmitCommands(_commandList);
                _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
            }
        }

        public void Destroy(){
            _graphicsDevice.WaitForIdle();
            _pipeline.Dispose();
            foreach(Shader s in _shaders){
                s.Dispose();
            }
            _commandList.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _imguiRenderer.Dispose();
            _image.Dispose();
            _resourceLayout.Dispose();
            _resourceSet.Dispose();
            _graphicsDevice.Dispose();
        }

        private const string VertexCode = @"
#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 0) out vec2 fsin_TexCoords;

void main()
{
    fsin_TexCoords = TexCoords;
    gl_Position = vec4(Position, 0, 1);
}";

        private const string FragmentCode = @"
#version 450

layout(set = 0, binding = 0) uniform texture2D Tex;
layout(set = 0, binding = 1) uniform texture2D Tex11;
layout(set = 0, binding = 2) uniform texture2D Tex22;
layout(set = 0, binding = 3) uniform sampler SS;

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 0) out vec4 OutColor;

void main()
{
    OutColor = texture(sampler2D(Tex, SS), fsin_TexCoords) + texture(sampler2D(Tex11, SS), fsin_TexCoords) * .01 + texture(sampler2D(Tex22, SS), fsin_TexCoords) * .01;
}";

    }
}