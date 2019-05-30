namespace Isolated.TestFramework.Events
{
    public class AppDomainEventListener
    {
        /// <summary>
        /// Occurs before a new app domain is loading. This is executed in the default app domain.
        /// </summary>
        public virtual void OnAppDomainLoading()
        {
        }

        /// <summary>
        /// Occurs after a new app domain was loaded. This is executed inside the new app domain. Only static code should be called here.
        /// </summary>
        public virtual void OnAppDomainLoadedRemotely()
        {
        }

        /// <summary>
        /// Occurs before an app domain is unloaded. This is executed inside the app domain. Only static code should be called here.
        /// </summary>
        public virtual void OnAppDomainUnloadingRemotely()
        {
        }

        /// <summary>
        /// Occurs after an app domain is unloaded. This is executed in the default app domain.
        /// </summary>
        public virtual void OnAppDomainUnloaded()
        {
        }
    }
}