using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace ShowOrderedBuildOutput
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(ShowOrderedBuildOutputPackage.PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class ShowOrderedBuildOutputPackage : AsyncPackage
    {
        /// <summary>
        /// ShowOrderedBuildOutputPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "d54b44ee-7a6a-4b2b-8538-d509a3bf021f";
        private IVsSolutionBuildManager buildManager;
        private uint bmCookie = VSConstants.VSCOOKIE_NIL;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await AutoShowOrderedBuildOutputCommand.InitializeAsync(this);

            var outputWindow = (IVsOutputWindow)await GetServiceAsync(typeof(SVsOutputWindow));
            if (outputWindow != null)
            {
                IVsOutputWindowPane buildOrderPane;
                outputWindow.GetPane(VSConstants.OutputWindowPaneGuid.SortedBuildOutputPane_guid, out buildOrderPane);

                if (buildOrderPane != null)
                {
                    buildManager = (IVsSolutionBuildManager)await GetServiceAsync(typeof(SVsSolutionBuildManager));
                    if (buildManager != null)
                    {
                        buildManager.AdviseUpdateSolutionEvents(new SolutionEventReceiver(buildOrderPane), out bmCookie);
                    }
                }
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            // Presumably we could get here via the Finalizer, so we don't want to throw if it's not the UI thread
            if (disposing)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally

                if (bmCookie != VSConstants.VSCOOKIE_NIL && buildManager != null)
                {
                    buildManager.UnadviseUpdateSolutionEvents(bmCookie);
                    bmCookie = VSConstants.VSCOOKIE_NIL;
                }
            }

            base.Dispose(disposing);
        }
    }
}
