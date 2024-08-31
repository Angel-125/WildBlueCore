using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueCore
{
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class DialogManager : MonoBehaviour
    {
        public static DialogManager Instance;

        bool showWindows = true;
        List<IManagedWindow> managedWindows = new List<IManagedWindow>();

        public void Awake()
        {
            Instance = this;

            GameEvents.onHideUI.Add(onHideUI);
            GameEvents.onShowUI.Add(onShowUI);
        }

        public void OnDestroy()
        {
            GameEvents.onHideUI.Remove(onHideUI);
            GameEvents.onShowUI.Remove(onShowUI);
        }

        public void OnGUI()
        {
            int totalWindows = managedWindows.Count;
            if (totalWindows == 0 || !showWindows)
                return;
            IManagedWindow managedWindow;

            for (int index = 0; index < totalWindows; index++)
            {
                managedWindow = managedWindows[index];
                if (managedWindow.IsVisible())
                    managedWindow.DrawWindow();
            }
        }

        public void RegisterWindow(IManagedWindow managedWindow)
        {
            if (managedWindows.Contains(managedWindow) == false)
                managedWindows.Add(managedWindow);
        }

        public void UnregisterWindow(IManagedWindow managedWindow)
        {
            if (managedWindows.Contains(managedWindow))
                managedWindows.Remove(managedWindow);
        }

        protected virtual void onHideUI()
        {
            showWindows = false;
        }

        protected virtual void onShowUI()
        {
            showWindows = true;
        }

    }
}
