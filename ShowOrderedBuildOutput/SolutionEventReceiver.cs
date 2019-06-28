using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ShowOrderedBuildOutput
{
    public class SolutionEventReceiver : IVsUpdateSolutionEvents
    {
        private readonly IVsOutputWindowPane buildOrderPane;

        public SolutionEventReceiver(IVsOutputWindowPane buildOrderPane)
        {
            this.buildOrderPane = buildOrderPane;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (buildOrderPane != null && Settings.Default.Checked)
            {
                buildOrderPane.Activate();
            }

            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }
    }
}
