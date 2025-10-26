using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using canmailbox.src.be;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmailbox.src.block
{
    public class CANBlockGenericTypedContainer : Block, IAttachableToEntity, IWearableShapeSupplier
    {
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

        // Token: 0x17000204 RID: 516
        // (get) Token: 0x06000FDA RID: 4058 RVA: 0x00099C97 File Offset: 0x00097E97
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

        // Token: 0x17000205 RID: 517
        // (get) Token: 0x06000FDB RID: 4059 RVA: 0x00099CB8 File Offset: 0x00097EB8
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

        // Token: 0x06000FDC RID: 4060 RVA: 0x00099CDC File Offset: 0x00097EDC
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            defaultType = Attributes["defaultType"].AsString("owl");
            variantByGroup = Attributes["variantByGroup"].AsString(null);
            variantByGroupInventory = Attributes["variantByGroupInventory"].AsString(null);
        }

        // Token: 0x06000FDD RID: 4061 RVA: 0x00099D48 File Offset: 0x00097F48
        public string GetType(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BEMailBox be = blockAccessor.GetBlockEntity(pos) as BEMailBox;
            if (be != null)
            {
                return be.type;
            }
            return defaultType;
        }

        // Token: 0x06000FDE RID: 4062 RVA: 0x00099D72 File Offset: 0x00097F72
        public override List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
        {
            return base.GetHandBookStacks(capi);
        }

        // Token: 0x06000FDF RID: 4063 RVA: 0x00099D7C File Offset: 0x00097F7C
        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BEMailBox bect = blockAccessor.GetBlockEntity(pos) as BEMailBox;
            if ((bect != null ? bect.collisionSelectionBoxes : null) != null)
            {
                return bect.collisionSelectionBoxes;
            }
            return base.GetCollisionBoxes(blockAccessor, pos);
        }

        // Token: 0x06000FE0 RID: 4064 RVA: 0x00099DB4 File Offset: 0x00097FB4
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
            Dictionary<string, MultiTextureMeshRef> meshrefs = new Dictionary<string, MultiTextureMeshRef>();
            string key = "genericTypedContainerMeshRefs" + FirstCodePart(0) + SubtypeInventory;
            meshrefs = ObjectCacheUtil.GetOrCreate(capi, key, delegate
            {
                foreach (KeyValuePair<string, MeshData> val in GenGuiMeshes(capi))
                {
                    meshrefs[val.Key] = capi.Render.UploadMultiTextureMesh(val.Value);
                }
                return meshrefs;
            });
            string type = itemstack.Attributes.GetString("type", defaultType);
            if (!meshrefs.TryGetValue(type, out renderinfo.ModelRef))
            {
                MeshData mesh = GenGuiMesh(capi, type);
                meshrefs[type] = renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(mesh);
            }
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
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
        private MeshData GenGuiMesh(ICoreClientAPI capi, string type)
        {
            string shapename = Attributes["shape"][type].AsString(null);
            return GenMesh(capi, type, shapename, null, null, 0);
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

        // Token: 0x06000FE7 RID: 4071 RVA: 0x0009A1B4 File Offset: 0x000983B4
        public Shape GetShape(ICoreAPI capi, string shapename)
        {
            if (shapename == null)
            {
                return null;
            }
            AssetLocation shapeloc = AssetLocation.Create(shapename, Code.Domain).WithPathPrefixOnce("shapes/");
            Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc + ".json");
            if (shape == null)
            {
                shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc + "1.json");
            }
            return shape;
        }

        // Token: 0x06000FE8 RID: 4072 RVA: 0x0009A214 File Offset: 0x00098414
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
            /*if (pos != null)
            {
                BEMailBox be = capi.World.BlockAccessor.GetBlockEntity(pos) as BEMailBox;
                Shape shape1;
                MeshData flagMesh;
                if (be != null)
                {
                    if (be.flagUp)
                    {
                        shape1 = capi.Assets.TryGet("canmailbox:shapes/flag_up.json").ToObject<Shape>();
                    }
                    else
                    {
                        shape1 = capi.Assets.TryGet("canmailbox:shapes/flag_down.json").ToObject<Shape>();
                    }
                    (capi as ICoreClientAPI).Tesselator.TesselateShape(this, shape1, out flagMesh);
                    mesh.AddMeshData(flagMesh);
                }
            }*/
            return mesh;
        }

        // Token: 0x06000FE9 RID: 4073 RVA: 0x0009A2F4 File Offset: 0x000984F4
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

        // Token: 0x06000FEA RID: 4074 RVA: 0x0009A43C File Offset: 0x0009863C
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")), 1);
            BEMailBox be = world.BlockAccessor.GetBlockEntity(pos) as BEMailBox;
            if (be != null)
            {
                stack.Attributes.SetString("type", be.type);
            }
            else
            {
                stack.Attributes.SetString("type", defaultType);
            }
            return stack;
        }

        // Token: 0x06000FEB RID: 4075 RVA: 0x0009A4AC File Offset: 0x000986AC
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
                world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, -0.5, byPlayer, true, 32f, 1f);
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

        // Token: 0x06000FEC RID: 4076 RVA: 0x0009A5EC File Offset: 0x000987EC
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

        // Token: 0x06000FED RID: 4077 RVA: 0x0009A6CA File Offset: 0x000988CA
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            return new ItemStack[]
            {
                new ItemStack(world.GetBlock(CodeWithVariant("side", "east")), 1)
            };
        }

        // Token: 0x06000FEE RID: 4078 RVA: 0x0009A6F1 File Offset: 0x000988F1
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            bool flag = true;
            /*if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }*/

            /*bool flag2 = false;
            BlockBehavior[] blockBehaviors = BlockBehaviors;
            foreach (BlockBehavior obj in blockBehaviors)
            {
                EnumHandling handling = EnumHandling.PassThrough;
                bool flag3 = obj.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
                if (handling != 0)
                {
                    flag = flag && flag3;
                    flag2 = true;
                }

                if (handling == EnumHandling.PreventSubsequent)
                {
                    return flag;
                }
            }

            if (flag2)
            {
                return flag;
            }

            return false;*/
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

        // Token: 0x06000FEF RID: 4079 RVA: 0x0009A6FC File Offset: 0x000988FC
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

        // Token: 0x06000FF0 RID: 4080 RVA: 0x0009A770 File Offset: 0x00098970
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

        // Token: 0x06000FF1 RID: 4081 RVA: 0x0009A820 File Offset: 0x00098A20
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

        // Token: 0x06000FF2 RID: 4082 RVA: 0x0009A8BB File Offset: 0x00098ABB
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

        // Token: 0x06000FF3 RID: 4083 RVA: 0x0009A8EB File Offset: 0x00098AEB
        public virtual bool IsAttachable(Entity toEntity, ItemStack itemStack)
        {
            if (toEntity is EntityPlayer)
            {
                return false;
            }
            ITreeAttribute attributes = itemStack.Attributes;
            return attributes == null || !attributes.HasAttribute("animalSerialized");
        }

        // Token: 0x040009B3 RID: 2483
        private string defaultType;

        // Token: 0x040009B4 RID: 2484
        private string variantByGroup;

        // Token: 0x040009B5 RID: 2485
        private string variantByGroupInventory;
    }
}
