using Cairo;
using canmailbox.src.be;
using canmailbox.src.block;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace canmailbox
{
    public class canmailbox : ModSystem
    {
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("CANBlockGenericTypedContainer", typeof(CANBlockGenericTypedContainer));
            api.RegisterBlockEntityClass("BEMailBox", typeof(BEMailBox));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {

        }
        public void AddCustomIcons(ICoreClientAPI api)
        {
            List<string> iconList = new List<string> { "save-arrow"};
            foreach (var icon in iconList)
            {
                api.Gui.Icons.CustomIcons["canmailbox:" + icon] = delegate (Context ctx, int x, int y, float w, float h, double[] rgba)
                {
                    AssetLocation location = new AssetLocation("canmailbox:textures/icons/" + icon + ".svg");
                    IAsset svgAsset = api.Assets.TryGet(location, true);
                    int value = ColorUtil.ColorFromRgba(175, 200, 175, 125);
                    api.Gui.DrawSvg(svgAsset, ctx.GetTarget() as ImageSurface, x, y, (int)w, (int)h, new int?(value));
                };
            }
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            AddCustomIcons(api);
            api.Event.TestBlockAccess += (IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, ref string claimant, EnumWorldAccessResponse response) =>
            {
                if (accessType == EnumBlockAccessFlags.Use && blockSel.Block != null && (blockSel.Block is CANBlockGenericTypedContainer))
                {
                    claimant = "";
                    return EnumWorldAccessResponse.Granted;
                }
                return response;
            };
        }
    }
}
