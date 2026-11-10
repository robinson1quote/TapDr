using Mastercam.App;
using Mastercam.App.Types;

namespace TapDR.Dovetail.Mastercam2027;

/// <summary>Mastercam 2027 NET-Hook entry point.</summary>
public sealed class TapDRDovetailStockGenerator : NetHook3App
{
    public override MCamReturn Run(int param)
    {
        try
        {
            using var form = new DovetailGeneratorForm();
            form.ShowDialog();
            return MCamReturn.NoErrors;
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(
                ex.GetBaseException().Message,
                "TapDR Dovetail Stock Generator",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
            return MCamReturn.ErrorOccurred;
        }
    }
}
