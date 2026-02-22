using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using canmailbox.src.be;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace canmailbox.src.block
{
    public class CANBlockGenericTypedContainer : Block, IAttachableToEntity, IWearableShapeSupplier, ITexPositionSource
    {
        private ITexPositionSource ownTextureSource;
        public ITexPositionSource tmpTextureSource;
        private ITextureAtlasAPI curAtlas;
        private string defaultType;
        private string variantByGroup;
        private string variantByGroupInventory;
        public string Subtype
        {
            get
            {
                if (variantByGroup != null)
                {
                    return Variant[variantByGroup];
                }
                return "";
            }
        }
        public string SubtypeInventory
        {
            get
            {
                if (variantByGroupInventory != null)
                {
                    return Variant[variantByGroupInventory];
                }
                return "";
            }
        }
        public int RequiresBehindSlots { get; set; }
        public Size2i AtlasSize { get; set; }
        public Dictionary<string, AssetLocation> tmpAssets = new Dictionary<string, AssetLocation>();
        Shape IWearableShapeSupplier.GetShape(ItemStack stack, Entity forEntity, string texturePrefixCode)
        {
            string type = stack.Attributes.GetString("type", null);
            string shapename = Attributes["shape"][type].AsString(null);
            Shape shape = GetShape(forEntity.World.Api, shapename);
            shape.SubclassForStepParenting(texturePrefixCode, 0f);
            return shape;
        }
        public int GetProvideSlots(ItemStack stack)
        {
            string type = stack.Attributes.GetString("type", null);
            if (type != null)
            {
                JsonObject itemAttributes = stack.ItemAttributes;
                int? num;
                if (itemAttributes == null)
                {
                    num = null;
                }
                else
                {
                    JsonObject jsonObject = itemAttributes["quantitySlots"];
                    if (jsonObject == null)
                    {
                        num = null;
                    }
                    else
                    {
                        JsonObject jsonObject2 = jsonObject[type];
                        num = jsonObject2 != null ? new int?(jsonObject2.AsInt(0)) : null;
                    }
                }
                int? num2 = num;
                return num2.GetValueOrDefault();
            }
            return 0;
        }
        public string GetCategoryCode(ItemStack stack)
        {
            ITreeAttribute attributes = stack.Attributes;
            string type = attributes != null ? attributes.GetString("type", null) : null;
            return Attributes["attachableCategoryCode"][type].AsString("chest");
        }
        public CompositeShape GetAttachedShape(ItemStack stack, string slotCode)
        {
            return null;
        }
        public void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
        {
            string type = stack.Attributes.GetString("type", null);
            foreach (string key in shape.Textures.Keys)
            {
                intoDict[texturePrefixCode + key] = Textures[type + "-" + key];
            }
        }
        public string[] GetDisableElements(ItemStack stack)
        {
            return null;
        }
        public string[] GetKeepElements(ItemStack stack)
        {
            return null;
        }
        public string GetTexturePrefixCode(ItemStack stack)
        {
            return Code.ToShortString() + "-" + stack.Attributes.GetString("type", null) + "-";
        }
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (tmpAssets.TryGetValue(textureCode, out var assetCode))
                {
                    return this.getOrCreateTexPos(assetCode);
                }

                Dictionary<string, CompositeTexture> dictionary;
                dictionary = new Dictionary<string, CompositeTexture>();
                foreach (var it in this.Textures)
                {
                    dictionary.Add(it.Key, it.Value);
                }
                AssetLocation texturePath = (AssetLocation)null;
                CompositeTexture compositeTexture;
                if (dictionary.TryGetValue(textureCode, out compositeTexture))
                    texturePath = compositeTexture.Baked.BakedName;
                if ((object)texturePath == null && dictionary.TryGetValue("all", out compositeTexture))
                    texturePath = compositeTexture.Baked.BakedName;

                return this.getOrCreateTexPos(texturePath);
            }
        }
        private TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
        {
            if(texturePath == null)
            {
                var c = 3;
            }
            TextureAtlasPosition texPos = (this.api as ClientCoreAPI).BlockTextureAtlas[texturePath];
            if (texPos == null)
            {
                IAsset asset = this.api.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                if (asset != null)
                {
                    BitmapRef bitmap = asset.ToBitmap((this.api as ClientCoreAPI));
                    (this.api as ClientCoreAPI).BlockTextureAtlas.InsertTextureCached(texturePath, (IBitmap)bitmap, out int _, out texPos);
                }
                else
                    (this.api as ClientCoreAPI).World.Logger.Warning("For render in block " + this.Code?.ToString() + ", item {0} defined texture {1}, not no such texture found.", "", (object)texturePath);
            }
            return texPos;
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            defaultType = Attributes["defaultType"].AsString("owl");
            variantByGroup = Attributes["variantByGroup"].AsString(null);
            variantByGroupInventory = Attributes["variantByGroupInventory"].AsString(null);
            AddAllTypesToCreativeInventory();
        }
        public void AddAllTypesToCreativeInventory()
        {
            List<JsonItemStack> stacks = new List<JsonItemStack>();
            Dictionary<string, string[]> vg = this.Attributes["variantGroups"].AsObject<Dictionary<string, string[]>>(null);
            if (this.Variant["side"] != "east")
            {
                return;
            }
            Random r = new Random();
            string[] woodTypes = ["birch", "oak", "maple", "pine", "acacia", "kapok", "baldcypress", "larch", "redwood", "ebony", "walnut", "purpleheart"];
            string[] metalTypes = ["copper", "cupronickel", "blackbronze", "tinbronze", "bismuthbronze", "iron", "meteoriciron", "gold", "silver", "steel"];
            foreach (string woodType in woodTypes)
            {
                foreach (var metalType in metalTypes)
                {
                    stacks.Add(this.genJstack(string.Format("{{ wood: \"{0}\", metal: \"{1}\", type: \"wallmounted-typed\" }}", woodType, metalType)));
                }
            }

            stacks.Add(this.genJstack(string.Format("{{ type: \"owl\" }}")));

            this.CreativeInventoryStacks = new CreativeTabAndStackList[]
            {
                new CreativeTabAndStackList
                {
                    Stacks = stacks.ToArray(),
                    Tabs = new string[]
                    {
                        "general",
                        "decorative"
                    }
                }
            };
        }
        private JsonItemStack genJstack(string json)
        {
            JsonItemStack jsonItemStack = new JsonItemStack();
            jsonItemStack.Code = this.Code;
            jsonItemStack.Type = EnumItemClass.Block;
            jsonItemStack.Attributes = new JsonObject(JToken.Parse(json));
            jsonItemStack.Resolve(this.api.World, "canmonocle type", true);
            return jsonItemStack;
        }
        public string GetType(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BEMailBox be = blockAccessor.GetBlockEntity(pos) as BEMailBox;
            if (be != null)
            {
                return be.type;
            }
            return defaultType;
        }
        public override List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
        {
            return base.GetHandBookStacks(capi);
        }
        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BEMailBox bect = blockAccessor.GetBlockEntity(pos) as BEMailBox;
            if ((bect != null ? bect.collisionSelectionBoxes : null) != null)
            {
                return bect.collisionSelectionBoxes;
            }
            return base.GetCollisionBoxes(blockAccessor, pos);
        }
        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BEMailBox bect = blockAccessor.GetBlockEntity(pos) as BEMailBox;
            if ((bect != null ? bect.collisionSelectionBoxes : null) != null)
            {
                return bect.collisionSelectionBoxes;
            }
            return base.GetSelectionBoxes(blockAccessor, pos);
        }
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (flag)
            {
                BEMailBox bect = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEMailBox;
                if (bect != null)
                {
                    BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    double y = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                    double dz = (double)(float)byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                    float angleHor = (float)Math.Atan2(y, dz);
                    string type = bect.type;
                    JsonObject attributes = Attributes;
                    string text;
                    if (attributes == null)
                    {
                        text = null;
                    }
                    else
                    {
                        JsonObject jsonObject = attributes["rotatatableInterval"][type];
                        text = jsonObject != null ? jsonObject.AsString("22.5deg") : null;
                    }
                    string a = text ?? "22.5deg";
                    if (a == "22.5degnot45deg")
                    {
                        float rounded90degRad = (int)Math.Round((double)(angleHor / 1.5707964f)) * 1.5707964f;
                        float deg45rad = 0.3926991f;
                        if (Math.Abs(angleHor - rounded90degRad) >= deg45rad)
                        {
                            bect.MeshAngle = rounded90degRad + 0.3926991f * Math.Sign(angleHor - rounded90degRad);
                        }
                        else
                        {
                            bect.MeshAngle = rounded90degRad;
                        }
                    }
                    if (a == "22.5deg")
                    {
                        float deg22dot5rad = 0.3926991f;
                        float roundRad = (int)Math.Round((double)(angleHor / deg22dot5rad)) * deg22dot5rad;
                        bect.MeshAngle = roundRad;
                        bect.MarkDirty();
                        if (world.Side == EnumAppSide.Client)
                        {
                            bect.OnFirstPlaced();
                        }
                    }
                }
            }
            if (flag)
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMailBox blockEntity)
                {
                    blockEntity.ownerUID = byPlayer.PlayerUID;
                    blockEntity.ownerName = byPlayer.PlayerName;
                }
            }
            return flag;
        }
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string blockMaterialCode = "iron";//this.GetBlockMaterialCode(itemstack);
            if (blockMaterialCode == null)
            {
                return;
            }
            string boxType = itemstack.Attributes.GetString("type", "owl");
            string woodType = itemstack.Attributes.GetString("wood", "oak");
            string metaType = itemstack.Attributes.GetString("metal", "iron");

            string key = "draw" + base.LastCodePart(0) + base.LastCodePart(1) + woodType + boxType + metaType;
            renderinfo.ModelRef = ObjectCacheUtil.GetOrCreate<MultiTextureMeshRef>(capi, key, delegate
            {
                AssetLocation shapeloc = this.Shape.Base;
                string type = itemstack.Attributes.GetString("type", "owl");
                if (type == "wallmounted-typed")
                {
                    shapeloc = new AssetLocation("canmailbox:shapes/postbox.json");
                }
                else if(type == "wallmounted")
                {
                    shapeloc = new AssetLocation("canmailbox:shapes/postbox.json");
                }
                else if (type == "owl")
                {
                    shapeloc = new AssetLocation("canmailbox:shapes/mudbox.json");
                }
                Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc).Clone();
                Block block = capi.World.GetBlock(new AssetLocation(blockMaterialCode));
                this.AtlasSize = capi.BlockTextureAtlas.Size;
                this.ownTextureSource = capi.Tesselator.GetTextureSource(this, 0, false);

                FillTextureDict(itemstack);
                MeshData meshdata;
                meshdata = GenMesh(capi, shape, null, this);
                return capi.Render.UploadMultiTextureMesh(meshdata);
            });
        }
        public void FillTextureDict(ItemStack itemstack)
        {
            string type = itemstack.Attributes.GetString("type", defaultType);
            string metalType = itemstack.Attributes.GetString("metal", "copper");
            string woodType = itemstack.Attributes.GetString("wood", "normal");
            if (type == "wallmounted-typed" && woodType != "normal" && woodType != "")
            {
                this.tmpAssets["wallmounted"] = new AssetLocation("game:block/metal/plate/iron.png");
                this.tmpAssets["black"] = new AssetLocation("game:block/black.png");
                this.tmpAssets["red"] = new AssetLocation("game:block/clay/hardened/red.png");
                this.tmpAssets["iron"] = new AssetLocation("game:block/metal/sheet/" + metalType + "1.png");
                this.tmpAssets["nickel"] = new AssetLocation("game:block/metal/plate/nickel.png");
                this.tmpAssets["iron5"] = new AssetLocation("game:block/metal/sheet/iron5.png");
                this.tmpAssets["molybdochalkos1"] = new AssetLocation("game:block/metal/sheet/" + metalType + "1.png");
                this.tmpAssets["inside"] = new AssetLocation("game:block/wood/planks/" + woodType + "1.png");
                this.tmpAssets["inner side"] = new AssetLocation("game:block/wood/planks/" + woodType + "1.png");
            }
            else if (type == "owl")
            {
                this.tmpAssets["top"] = new AssetLocation("game:block/wood/chest/owl/top.png");
                this.tmpAssets["sides2"] = new AssetLocation("game:block/wood/chest/owl/sides2.png");
                this.tmpAssets["sides1"] = new AssetLocation("game:block/wood/chest/owl/sides1.png");
                this.tmpAssets["inside"] = new AssetLocation("game:block/wood/chest/owl/inside.png");

                this.tmpAssets["copper"] = new AssetLocation("game:block/metal/plate/copper.png");
                this.tmpAssets["aged"] = new AssetLocation("game:block/wood/debarked/aged.png");
                this.tmpAssets["iron"] = new AssetLocation("game:block/metal/plate/iron.png");
            }
            else if (type == "wallmounted" || type == "wallmounted-typed" || woodType == "normal")
            {
                this.tmpAssets["wallmounted"] = new AssetLocation("game:block/metal/plate/iron.png");
                this.tmpAssets["black"] = new AssetLocation("game:block/black.png");
                this.tmpAssets["red"] = new AssetLocation("game:block/clay/hardened/red.png");
                this.tmpAssets["iron"] = new AssetLocation("game:block/metal/plate/iron.png");
                this.tmpAssets["nickel"] = new AssetLocation("game:block/metal/plate/nickel.png");
                this.tmpAssets["iron5"] = new AssetLocation("game:block/metal/sheet/iron5.png");
                this.tmpAssets["molybdochalkos1"] = new AssetLocation("game:block/metal/sheet/molybdochalkos1.png");
                this.tmpAssets["inside"] = new AssetLocation("game:block/wood/barrel/inside.png");
                this.tmpAssets["inner side"] = new AssetLocation("game:block/wood/bucket/inner side.png");
            }
            
        }
        public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
        {
            base.OnDecalTesselation(world, decalMesh, pos);
        }
        public override void OnUnloaded(ICoreAPI api)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
            if (capi == null)
            {
                return;
            }
            string key = "genericTypedContainerMeshRefs" + FirstCodePart(0) + SubtypeInventory;
            Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, key);
            if (meshrefs != null)
            {
                foreach (KeyValuePair<string, MultiTextureMeshRef> val in meshrefs)
                {
                    val.Value.Dispose();
                }
                capi.ObjectCache.Remove(key);
            }
        }
        public MeshData GenMesh(ICoreClientAPI capi, Shape shape = null, ITesselatorAPI tesselator = null, ITexPositionSource textureSource = null, string part = "", Vec3f rotationDeg = null)
        {
            if (tesselator == null)
            {
                tesselator = capi.Tesselator;
            }
            curAtlas = capi.BlockTextureAtlas;
            if (textureSource != null)
            {
                tmpTextureSource = textureSource;
            }
            else
            {
                tmpTextureSource = tesselator.GetTextureSource(this);
            }
            AtlasSize = capi.BlockTextureAtlas.Size;
            tesselator.TesselateShape("mailbox", shape, out var modeldata, this, rotationDeg, 0, 0, 0);
            return modeldata;
        }
        public Dictionary<string, MeshData> GenGuiMeshes(ICoreClientAPI capi)
        {
            string[] array = Attributes["types"].AsArray<string>(null, null);
            Dictionary<string, MeshData> meshes = new Dictionary<string, MeshData>();
            foreach (string type in array)
            {
                string shapename = Attributes["shape"][type].AsString(null);// + "_down";
                meshes[type] = GenMesh(capi, type, shapename, null, ShapeInventory == null ? null : new Vec3f(ShapeInventory.rotateX, ShapeInventory.rotateY, ShapeInventory.rotateZ), 0);
            }
            return meshes;
        }
        public Shape GetShape(ICoreAPI capi, string shapename)
        {
            if (shapename == null)
            {
                return null;
            }
            AssetLocation shapeloc = AssetLocation.Create(shapename, Code.Domain).WithPathPrefixOnce("shapes/");
            Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc + ".json").Clone();
            if (shape == null)
            {
                shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc + "1.json");
            }
            return shape;
        }
        public MeshData GenMesh(ICoreClientAPI capi, string type, string shapename, ITesselatorAPI tesselator = null, Vec3f rotation = null, int altTexNumber = 0)
        {
            Shape shape = GetShape(capi, shapename);
            if (tesselator == null)
            {
                tesselator = capi.Tesselator;
            }
            if (shape == null)
            {
                capi.Logger.Warning("Container block {0}, type: {1}: Shape file {2} not found!", new object[]
                {
                    Code,
                    type,
                    shapename
                });
                return new MeshData(true);
            }
            GenericContainerTextureSource texSource = new GenericContainerTextureSource
            {
                blockTextureSource = tesselator.GetTextureSource(this, altTexNumber, false),
                curType = type
            };
            TesselationMetaData meta = new TesselationMetaData
            {
                TexSource = texSource,
                WithJointIds = true,
                WithDamageEffect = true,
                TypeForLogging = "typedcontainer",
                Rotation = rotation == null ? new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ) : rotation
            };

            MeshData mesh;
            tesselator.TesselateShape(meta, shape, out mesh);
            return mesh;
        }
        public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
        {
            BEMailBox be = world.BlockAccessor.GetBlockEntity(pos) as BEMailBox;
            if (be == null)
            {
                base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
                return;
            }
            ICoreClientAPI capi = api as ICoreClientAPI;
            string shapename = Attributes["shape"][be.type].AsString(null);// + "_down";
            if (shapename == null)
            {
                base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
                return;
            }
            blockModelData = GenMesh(capi, be.type, shapename, null, null, 0);
            AssetLocation shapeloc = AssetLocation.Create(shapename, Code.Domain).WithPathPrefixOnce("shapes/");
            Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc + ".json");
            if (shape == null)
            {
                shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc + "1.json");
            }
            GenericContainerTextureSource texSource = new GenericContainerTextureSource
            {
                blockTextureSource = decalTexSource,
                curType = be.type
            };
            MeshData md;
            capi.Tesselator.TesselateShape("typedcontainer-decal", shape, out md, texSource, null, 0, 0, 0, null, null);
            decalModelData = md;
            decalModelData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, be.MeshAngle, 0f);
        }
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")), 1);
            BEMailBox be = world.BlockAccessor.GetBlockEntity(pos) as BEMailBox;
            if (be != null)
            {
                stack.Attributes.SetString("type", be.type);
                stack.Attributes.SetString("wood", be.woodType);
                stack.Attributes.SetString("metal", be.metalType);
            }
            else
            {
                stack.Attributes.SetString("type", defaultType);
            }
            return stack;
        }
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            bool preventDefault = false;
            foreach (BlockBehavior blockBehavior in BlockBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;
                blockBehavior.OnBlockBroken(world, pos, byPlayer, ref handled);
                if (handled == EnumHandling.PreventDefault)
                {
                    preventDefault = true;
                }
                if (handled == EnumHandling.PreventSubsequent)
                {
                    return;
                }
            }
            if (preventDefault)
            {
                return;
            }
            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                ItemStack[] drops = new ItemStack[]
                {
                    OnPickBlock(world, pos)
                };
                JsonObject jsonObject = Attributes["drop"];
                bool flag;
                if (jsonObject == null)
                {
                    flag = false;
                }
                else
                {
                    JsonObject jsonObject2 = jsonObject[GetType(world.BlockAccessor, pos)];
                    flag = (jsonObject2 != null ? new bool?(jsonObject2.AsBool(false)) : null).GetValueOrDefault();
                }
                if (flag && drops != null)
                {
                    for (int i = 0; i < drops.Length; i++)
                    {
                        world.SpawnItemEntity(drops[i], pos, null);
                    }
                }
                world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, 0, byPlayer, 1f);
            }
            if (EntityClass != null)
            {
                BlockEntity entity = world.BlockAccessor.GetBlockEntity(pos);
                if (entity != null)
                {
                    entity.OnBlockBroken(null);
                }
            }
            world.BlockAccessor.SetBlock(0, pos);
        }
        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            ITreeAttribute attributes = handbookStack.Attributes;
            string type = attributes != null ? attributes.GetString("type", null) : null;
            if (type == null)
            {
                ILogger logger = api.World.Logger;
                string str = "BlockGenericTypedContainer.GetDropsForHandbook(): type not set for block ";
                CollectibleObject collectible = handbookStack.Collectible;
                logger.Warning(str + (collectible != null ? collectible.Code : null));
                return new BlockDropItemStack[0];
            }
            JsonObject attributes2 = Attributes;
            bool flag;
            if (attributes2 == null)
            {
                flag = false;
            }
            else
            {
                JsonObject jsonObject = attributes2["drop"];
                bool? flag2;
                if (jsonObject == null)
                {
                    flag2 = null;
                }
                else
                {
                    JsonObject jsonObject2 = jsonObject[type];
                    flag2 = jsonObject2 != null ? new bool?(jsonObject2.AsBool(false)) : null;
                }
                bool? flag3 = flag2;
                bool flag4 = false;
                flag = flag3.GetValueOrDefault() == flag4 & flag3 != null;
            }
            if (flag)
            {
                return new BlockDropItemStack[0];
            }
            return new BlockDropItemStack[]
            {
                new BlockDropItemStack(handbookStack, 1f)
            };
        }
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            return new ItemStack[]
            {
                new ItemStack(world.GetBlock(CodeWithVariant("side", "east")), 1)
            };
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BEMailBox be = null;
            if (blockSel.Position != null)
            {
                be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEMailBox;
            }

            if (blockSel.Position != null)
            {
                if (be != null)
                {
                    be.OnPlayerRightClick(byPlayer, blockSel);
                }

                return true;
            }

            return false;
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
        public override string GetHeldItemName(ItemStack itemStack)
        {
            string type = itemStack.Attributes.GetString("type", null);
            string[] array = new string[5];
            int num = 0;
            AssetLocation code = Code;
            array[num] = code != null ? code.Domain : null;
            array[1] = ":block-";
            array[2] = type;
            array[3] = "-";
            int num2 = 4;
            AssetLocation code2 = Code;
            array[num2] = code2 != null ? code2.Path : null;
            return Lang.GetMatching(string.Concat(array), Array.Empty<object>());
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            string type = inSlot.Itemstack.Attributes.GetString("type", null);
            if (type != null)
            {
                JsonObject itemAttributes = inSlot.Itemstack.ItemAttributes;
                int? num;
                if (itemAttributes == null)
                {
                    num = null;
                }
                else
                {
                    JsonObject jsonObject = itemAttributes["quantitySlots"];
                    if (jsonObject == null)
                    {
                        num = null;
                    }
                    else
                    {
                        JsonObject jsonObject2 = jsonObject[type];
                        num = jsonObject2 != null ? new int?(jsonObject2.AsInt(0)) : null;
                    }
                }
                int? qslots = num;
                dsc.AppendLine("\n" + Lang.Get("Storage Slots: {0}", new object[]
                {
                    qslots
                }));
            }
        }
        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
        {
            BEMailBox be = capi.World.BlockAccessor.GetBlockEntity(pos) as BEMailBox;
            if (be != null)
            {
                CompositeTexture tex = null;
                if (!Textures.TryGetValue(be.type + "-lid", out tex))
                {
                    Textures.TryGetValue(be.type + "-top", out tex);
                }
                return capi.BlockTextureAtlas.GetRandomColor((tex != null ? tex.Baked : null) == null ? 0 : tex.Baked.TextureSubId, rndIndex);
            }
            return base.GetRandomColor(capi, pos, facing, rndIndex);
        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "blockhelp-chest-open",
                    MouseButton = EnumMouseButton.Right
                }
            }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
        public virtual bool IsAttachable(Entity toEntity, ItemStack itemStack)
        {
            if (toEntity is EntityPlayer)
            {
                return false;
            }
            ITreeAttribute attributes = itemStack.Attributes;
            return attributes == null || !attributes.HasAttribute("animalSerialized");
        }
    }
}
