using System.Drawing;
using System.Windows.Forms;
using SpeakerKeepAwake.UI;

namespace SpeakerKeepAwake.Tests.UI;

public sealed class TrayContextMenuTests
{
    [Fact]
    public void PresentsCompactCommandsWithoutTheLegacyCheckGutter()
    {
        RunInSta(() =>
        {
            using var menu = new TrayContextMenu(ThemePalette.Create(isDark: true), protectionEnabled: true);

            Assert.False(menu.ShowCheckMargin);
            Assert.True(menu.ShowImageMargin);
            Assert.IsType<ToolStripProfessionalRenderer>(menu.Renderer);
            Assert.Equal("Abrir configurações", menu.OpenItem.Text);
            Assert.Equal("Proteção ligada", menu.ProtectionItem.Text);
            Assert.Equal("Sair", menu.ExitItem.Text);
            Assert.NotNull(menu.OpenItem.Image);
            Assert.NotNull(menu.ProtectionItem.Image);
            Assert.NotNull(menu.ExitItem.Image);
            Assert.Equal(new Size(16, 16), menu.ImageScalingSize);
            Assert.InRange(menu.MinimumSize.Width, 180, 204);

            var commands = new[] { menu.OpenItem, menu.ProtectionItem, menu.ExitItem };
            Assert.All(commands, item =>
            {
                Assert.InRange(item.Height, 28, 32);
                Assert.Equal(ContentAlignment.MiddleLeft, item.TextAlign);
                Assert.Equal(ContentAlignment.MiddleCenter, item.ImageAlign);
                Assert.Equal(menu.Font.Size, item.Font.Size);
                Assert.Equal(FontStyle.Regular, item.Font.Style);
                Assert.Equal(16, item.Image!.Width);
                Assert.Equal(16, item.Image.Height);
            });
        });
    }

    [Fact]
    public void UpdatesProtectionCommandWhenStateChanges()
    {
        RunInSta(() =>
        {
            using var menu = new TrayContextMenu(ThemePalette.Create(isDark: false), protectionEnabled: true);

            menu.SetProtectionEnabled(false);

            Assert.Equal("Proteção desligada", menu.ProtectionItem.Text);
        });
    }

    private static void RunInSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception exception) { error = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null) throw error;
    }
}
