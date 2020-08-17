//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//*********************************************************

using SoftdocMusicPlayer.Samples.LightInterop;
using Windows.UI.Xaml.Controls;

namespace SoftdocMusicPlayer
{
    public sealed partial class LightInterop : Page
    {
        public LightInterop()
        {
            this.InitializeComponent();

            // Target Grid with lights in code behind because SDK MinVersion > 14393 is needed for <Grid.Ligts> in markup (see .xaml file for comments)
            BackdropGrid.Lights.Add(new HoverLight());
            BackdropGrid.Lights.Add(new AmbLight());
        }
    }
}