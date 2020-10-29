using Gallio.Common;
using Gallio.Icarus.WindowManager;
using Rhino.Mocks;

namespace Gallio.Icarus.Tests.WindowManager
{
    public class TestWindowManager
    {
        public static IWindowManager Create()
        {
            var windowManager = MockRepository.GenerateStub<IWindowManager>();
            windowManager.Stub(wm => wm.Register(Arg<string>.Is.Anything, Arg<GallioAction>.Is.Anything, Arg<Location>.Is.Anything))
                .Do((GallioAction<string, GallioAction, Location>)((i, a, l) => a()));
            return windowManager;
        }
    }
}
