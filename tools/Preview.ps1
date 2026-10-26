param([string]$SimHubDir = 'C:\Program Files (x86)\SimHub')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Windows.Forms
$resolver = [System.ResolveEventHandler]{
    param($sender, $eventArgs)
    $name = ([Reflection.AssemblyName]::new($eventArgs.Name)).Name
    $path = Join-Path $SimHubDir ($name + '.dll')
    if (Test-Path -LiteralPath $path) { return [Reflection.Assembly]::LoadFrom($path) }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
try {
    $dll = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\Release\osats.WLED.dll'))
    $plugin = [Activator]::CreateInstance($dll.GetType('osats.WLED.WLED'))
    $settings = [Activator]::CreateInstance($dll.GetType('osats.WLED.WLEDSettings'))
    $settings.Migrate()
    $plugin.Settings = $settings
    $control = [Activator]::CreateInstance($dll.GetType('osats.WLED.SettingsControl'), [object[]]@($plugin))
    if ($control.FindName('Editor').Children.Count -ne 3) { throw 'Expected three configuration sections.' }
    $effects = $control.FindName('Editor').Children[2].Content
    if ($effects.Children.Count -ne 9) { throw 'Expected eight effect cards and a hint.' }
    $effects.Children[3].IsExpanded = $true
    $control.Background = [Windows.Media.Brushes]::White
    $control.Foreground = [Windows.Media.Brushes]::Black
    $control.Measure([Windows.Size]::new(900, 1300))
    $control.Arrange([Windows.Rect]::new(0, 0, 900, 1300))
    $control.UpdateLayout()
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(900, 1300, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($control)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.File]::Create((Join-Path $PSScriptRoot '..\artifacts\configurator.png'))
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
    Write-Output 'PASS: WPF configurator instantiated and rendered.'
} finally { [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver) }
