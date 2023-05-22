using Dragonfly.Graphics.API.Directx9;
using Dragonfly.Graphics.API.Directx11;
using Dragonfly.Graphics.API.Directx12;
using System.Collections.Generic;
using System;

namespace Dragonfly.Graphics
{
    public static class GraphicsAPIs
    {
        private static List<IGraphicsAPI> allAPI; // the default is at index 0

        static GraphicsAPIs()
        {
            allAPI = new List<IGraphicsAPI>();
            allAPI.Add(new Directx9API());
            allAPI.Add(new Directx11API());
            allAPI.Add(new Directx12API());
        }

        public static IGraphicsAPI GetDefault()
        {
            return allAPI[0];
        }

        public static void SetDefault(IGraphicsAPI api)
        {
            Add(api, true);
        }

        public static void Add(IGraphicsAPI api, bool asDefault)
        {
            Predicate<IGraphicsAPI> sameApi = (IGraphicsAPI x) => { return x.Description == api.Description; };
            if (allAPI.Exists(sameApi))
                allAPI.RemoveAll(sameApi);

            if (asDefault) allAPI.Insert(0, api);
            else allAPI.Add(api);
        }

        public static List<IGraphicsAPI> GetList()
        {
            return new List<IGraphicsAPI>(allAPI);
        }

    }

}
