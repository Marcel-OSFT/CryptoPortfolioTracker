using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.ApplicationSettings;
using Windows.UI.Text;

namespace CryptoPortfolioTracker.Models
{
    public partial class BaseModel : INotifyPropertyChanged
    {

        //***** Constructor     
        public BaseModel()
        {
           
        }

        public XamlUICommand InitXamlUICommand(XamlUICommand command, string glyph, FontWeight fontWeight, string description, string label, Windows.System.VirtualKey shortcutKey = Windows.System.VirtualKey.None, Windows.System.VirtualKeyModifiers shortcutModifier = Windows.System.VirtualKeyModifiers.None)
        {
            command.Description = description;
            command.Label = label;
            FontIconSource fontIconSource = new FontIconSource
            {
                Glyph = glyph,
                FontWeight = fontWeight
            };
            command.IconSource = fontIconSource;

            if (shortcutKey != Windows.System.VirtualKey.None)
            {
                var shortcut = new KeyboardAccelerator
                {
                    Modifiers = shortcutModifier,
                    Key = shortcutKey
                };
                command.KeyboardAccelerators.Add(shortcut);
            }
            return command;

        }
        public StandardUICommand InitStandardUICommand(StandardUICommand command, StandardUICommandKind commandKind, string description, string label)
        {
            command.Kind = commandKind;
            command.Description = description;
            command.Label = label;

            return command;
        }

        //******* EventHandlers
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (MainPage.Current == null) return;
            MainPage.Current.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                //Debug.WriteLine("OnPropertyChanged (BaseModel) => " + name);

                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            });
        }

        //public event PropertyChangedEventHandler? PropertyChanged;

        //protected void OnPropertyChanged(string propertyName)
        //{
        //    VerifyPropertyName(propertyName);
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        //[Conditional("DEBUG")]
        //private void VerifyPropertyName(string propertyName)
        //{
        //    if (TypeDescriptor.GetProperties(this)[propertyName] == null)
        //        throw new ArgumentNullException(GetType().Name + " does not contain property: " + propertyName);
        //}
    }
}

