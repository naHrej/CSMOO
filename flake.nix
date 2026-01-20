{
  description = "CricketSim dev shell";

  
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { self, nixpkgs }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
    in
    {
      devShells.${system}.default = pkgs.mkShell {
        packages = with pkgs; [
          dotnet-sdk_9
          fontconfig
          freetype
          icu
          libGL
          libdrm
          libxkbcommon
          skia
          SDL2
          wayland
          xorg.libX11
          xorg.libXcursor
          xorg.libICE
          xorg.libSM
          xorg.libXi
          xorg.libXrandr
          xorg.libXrender
          xorg.libXext
          xorg.libxcb
        ];

        DOTNET_ROOT = "${pkgs.dotnet-sdk_10}";
        shellHook = ''
          export DOTNET_CLI_HOME="$PWD/.dotnet"
          export NUGET_PACKAGES="$PWD/.nuget/packages"
          export FONTCONFIG_PATH="${pkgs.fontconfig.out}/etc/fonts"
          export LD_LIBRARY_PATH="${pkgs.lib.makeLibraryPath [
            pkgs.fontconfig
            pkgs.freetype
            pkgs.icu
            pkgs.libGL
            pkgs.libdrm
            pkgs.libxkbcommon
            pkgs.skia
            pkgs.SDL2
            pkgs.wayland
            pkgs.xorg.libX11
            pkgs.xorg.libXcursor
            pkgs.xorg.libICE
            pkgs.xorg.libSM
            pkgs.xorg.libXi
            pkgs.xorg.libXrandr
            pkgs.xorg.libXrender
            pkgs.xorg.libXext
            pkgs.xorg.libxcb
          ]}:$LD_LIBRARY_PATH"
        '';
      };
    };
}
