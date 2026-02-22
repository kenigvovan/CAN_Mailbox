using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using canmailbox.src.block;
using canmailbox.src.gui;
using canmailbox.src.inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canmailbox.src.be
{
    public class BEMailBox: BlockEntityOpenableContainer, IRotatable, ITexPositionSource
    {
        internal InventoryCANMailBox inventory;
        public string type = "owl";
        public string woodType = "normal";
        public string metalType = "iron";
        public string defaultType;
        public int quantitySlots = 16;
        public int quantityColumns = 4;
        public string inventoryClassName = "canmailbox";
        public string dialogTitleLangCode = "chestcontents";
        public bool retrieveOnly;
        private float meshangle;
        public MeshData ownMesh;
        public Cuboidf[] collisionSelectionBoxes;
        private Vec3f rendererRot = new Vec3f();
        public string ownerUID;
        public string ownerName;
        public bool flagUp = false;
        bool flagChecked = false;
        public virtual float MeshAngle
        {
            get
            {
                return this.meshangle;
            }
            set
            {
                this.meshangle = value;
                this.rendererRot.Y = value * 57.295776f;
            }
        }
        public virtual string DialogTitle
        {
            get
            {
                return Lang.Get(this.dialogTitleLangCode, Array.Empty<object>());
            }
        }
        public override InventoryCANMailBox Inventory
        {
            get
            {
                return this.inventory;
            }
        }
        public override string InventoryClassName
        {
            get
            {
                return this.inventoryClassName;
            }
        }
        private BlockEntityAnimationUtil animUtil
        {
            get
            {
                BEBehaviorAnimatable behavior = base.GetBehavior<BEBehaviorAnimatable>();
                if (behavior == null)
                {
                    return null;
                }
                return behavior.animUtil;
            }
        }
        private ICoreClientAPI capi;
        public Dictionary<string, AssetLocation> tmpAssets = new Dictionary<string, AssetLocation>();
        public Size2i AtlasSize => this.capi.BlockTextureAtlas.Size;
        public override void Dispose()
        {
            GuiDialogBlockEntity guiDialogBlockEntity = invDialog;
            if (guiDialogBlockEntity != null && guiDialogBlockEntity.IsOpened())
            {
                invDialog?.TryClose();
            }

            invDialog?.Dispose();
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            IClientWorldAccessor clientWorldAccessor = (IClientWorldAccessor)Api.World;
            if (packetid == 5000)
            {
                return;
                if (invDialog != null)
                {
                    GuiDialogBlockEntity guiDialogBlockEntity = invDialog;
                    if (guiDialogBlockEntity != null && guiDialogBlockEntity.IsOpened())
                    {
                        invDialog.TryClose();
                    }

                    invDialog?.Dispose();
                    invDialog = null;
                    return;
                }

                BlockEntityContainerOpen blockEntityContainerOpen = BlockEntityContainerOpen.FromBytes(data);
                Inventory.FromTreeAttributes(blockEntityContainerOpen.Tree);
                Inventory.ResolveBlocksOrItems();
                invDialog = new GuiDialogBlockEntityMailBoxInventory(blockEntityContainerOpen.DialogTitle, Inventory, Pos, blockEntityContainerOpen.Columns, Api as ICoreClientAPI);
                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                string text = block.Attributes?["openSound"]?.AsString();
                string text2 = block.Attributes?["closeSound"]?.AsString();
                AssetLocation assetLocation = ((text == null) ? null : AssetLocation.Create(text, block.Code.Domain));
                AssetLocation assetLocation2 = ((text2 == null) ? null : AssetLocation.Create(text2, block.Code.Domain));
                invDialog.OpenSound = assetLocation ?? OpenSound;
                invDialog.CloseSound = assetLocation2 ?? CloseSound;
                invDialog.TryOpen();
            }

            if (packetid == 5001)
            {
                OpenContainerLidPacket openContainerLidPacket = SerializerUtil.Deserialize<OpenContainerLidPacket>(data);
                if (this is BEMailBox blockEntityGenericTypedContainer)
                {
                    if (openContainerLidPacket.Opened)
                    {
                        LidOpenEntityId.Add(openContainerLidPacket.EntityId);
                        blockEntityGenericTypedContainer.OpenLid();
                    }
                    else
                    {
                        LidOpenEntityId.Remove(openContainerLidPacket.EntityId);
                        if (LidOpenEntityId.Count == 0)
                        {
                            blockEntityGenericTypedContainer.CloseLid();
                        }
                    }
                }
            }

            if (packetid == 1001)
            {
                clientWorldAccessor.Player.InventoryManager.CloseInventory(Inventory);
                GuiDialogBlockEntity guiDialogBlockEntity2 = invDialog;
                if (guiDialogBlockEntity2 != null && guiDialogBlockEntity2.IsOpened())
                {
                    invDialog?.TryClose();
                }

                invDialog?.Dispose();
                invDialog = null;
            }
            
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
                foreach (var it in this.Block.Textures)
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
            TextureAtlasPosition texPos = this.capi.BlockTextureAtlas[texturePath];
            if (texPos == null)
            {
                IAsset asset = this.capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                if (asset != null)
                {
                    BitmapRef bitmap = asset.ToBitmap(this.capi);
                    this.capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out int _, out texPos, () => asset.ToBitmap(this.Api as ICoreClientAPI));
                }
                else
                {
                    this.capi.World.Logger.Warning("For render in block " + this.Block.Code?.ToString() + ", item {0} defined texture {1}, not no such texture found.", "", (object)texturePath);
                }
            }
            return texPos;
        }
        public BEMailBox()
        {
        }
        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            base.CreateBehaviors(block, worldForResolve);
            var f = 3;
        }
        public string GetShapeString()
        {
            string shapeloc;
            if (type == "wallmounted-typed")
            {
                shapeloc = "canmailbox:shapes/postbox.json";
            }
            else if (type == "wallmounted")
            {
                shapeloc = "canmailbox:shapes/postbox.json";
            }
            else
            {
                shapeloc = "canmailbox:shapes/mudbox.json";
            }
            return shapeloc;
        }
        public override void Initialize(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Server)
            {
                //this.MeshAngle = (BlockFacing.FromCode(base.Block.LastCodePart(0)).HorizontalAngleIndex - 1) * 90;
            }
            else
            {
                this.capi = api as ICoreClientAPI;
            }
            JsonObject attributes = base.Block.Attributes;
            string text;
            if (attributes == null)
            {
                text = null;
            }
            else
            {
                JsonObject jsonObject = attributes["defaultType"];
                text = ((jsonObject != null) ? jsonObject.AsString("normal-generic") : null);
            }
            this.defaultType = text;
            if (this.defaultType == null)
            {
                this.defaultType = "owl";
            }
            if (this.inventory == null)
            {
                this.InitInventory(base.Block);
                //this.inventory.LateInitialize("canmailbox-" + this.Pos.X.ToString() + "/" + this.Pos.Y.ToString() + "/" + this.Pos.Z.ToString(), api, this);
            }
            this.inventory.LateInitialize("canmailbox-" + this.Pos.X.ToString() + "/" + this.Pos.Y.ToString() + "/" + this.Pos.Z.ToString(), api, this);
            base.Initialize(api);
            if (this.Api.Side == EnumAppSide.Client)
            {
                if (!this.flagChecked)
                {
                    if (flagUp)
                    {
                        this.ownMesh = this.GenMesh((Api as ICoreClientAPI).Tesselator, true);
                        this.MarkDirty(true);
                        this.animUtil.StartAnimation(new AnimationMetaData
                        {
                            Animation = "flagup",
                            Code = "flagup",
                            AnimationSpeed = 1.8f,
                            EaseOutSpeed = 6f,
                            EaseInSpeed = 15f
                        });
                    }
                    this.flagChecked = true;
                    if (this.ownMesh == null && woodType is not null)
                    {
                        //this.ownMesh = this.GenMesh(this.capi.Tesselator);
                        string key = GetMeshKey();
                        this.FillTextureDict();
                        
                        this.ownMesh = animUtil.InitializeAnimator(string.Concat(new string[]
                        {
                            key,
                            "-",
                            flagUp.ToString()
                        }), Vintagestory.API.Common.Shape.TryGet(this.Api, GetShapeString()), this, this.rendererRot);
                    }
                }
                else
                {
                    if (this.ownMesh == null && woodType is not null)
                    {
                        FillTextureDict();
                        this.ownMesh = this.GenMesh(this.capi.Tesselator);
                        string key = GetMeshKey();
                        this.ownMesh = animUtil.InitializeAnimator(string.Concat(new string[]
                        {
                            key,
                            "-",
                            flagUp.ToString()
                        }), Vintagestory.API.Common.Shape.TryGet(this.Api, GetShapeString()), this, this.rendererRot);
                        
                    }
                }
            }
        }
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (((byItemStack != null) ? byItemStack.Attributes : null) != null)
            {
                string nowType = byItemStack.Attributes.GetString("type", this.defaultType);
                if (nowType != this.type)
                {
                    this.type = nowType;
                    this.InitInventory(base.Block);
                    this.LateInitInventory();
                }
                string nowWoodType = byItemStack.Attributes.GetString("wood", "normal");
                string nowMetalType = byItemStack.Attributes.GetString("metal", "iron");
                this.metalType = nowMetalType;
                if (nowWoodType != this.woodType)
                {
                    this.woodType = nowWoodType;
                }
                if (this.Api.Side == EnumAppSide.Client && woodType is not null)
                {
                    FillTextureDict();
      
                    string skey = string.Concat(new string[]
                    {
                        base.Block.FirstCodePart(0),
                        this.woodType,
                        this.type
                    });
                    string key = GetMeshKey();
                    this.ownMesh = animUtil.InitializeAnimator("wiring2" + string.Concat(new string[]
                    {
                            key,
                            "-",
                            flagUp.ToString()
                    }), Vintagestory.API.Common.Shape.TryGet(this.Api, GetShapeString()), this, this.rendererRot);
                }
            }
            base.OnBlockPlaced(null);
        }
        public void OnFirstPlaced()
        {
            string key = GetMeshKey();
            this.ownMesh = animUtil.InitializeAnimator(string.Concat(new string[]
                    {
                            key,
                            "-",
                            flagUp.ToString()
                    }), Vintagestory.API.Common.Shape.TryGet(this.Api, GetShapeString()), this, this.rendererRot);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            string prevType = this.type;
            this.type = tree.GetString("type", this.defaultType);
            this.MeshAngle = tree.GetFloat("meshAngle", this.MeshAngle);
            this.ownerUID = tree.GetString("ownerUID");
            this.ownerName = tree.GetString("ownerName");
            bool typeChanged = false;
            if (this.inventory == null)
            {
                if (tree.HasAttribute("forBlockId"))
                {
                    this.InitInventory(worldForResolving.GetBlock((int)((ushort)tree.GetInt("forBlockId", 0))));
                    this.LateInitInventory();
                }
                else if (tree.HasAttribute("forBlockCode"))
                {
                    this.InitInventory(worldForResolving.GetBlock(new AssetLocation(tree.GetString("forBlockCode", null))));
                    string str = this.InventoryClassName;
                    string str2 = "-";
                    BlockPos pos = this.Pos;
                    this.inventory.LateInitialize(str + str2 + ((pos != null) ? pos.ToString() : null), worldForResolving.Api, this);
                }
                else
                {
                    if (tree.GetTreeAttribute("inventory").GetInt("qslots", 0) == 8)
                    {
                        this.quantitySlots = 8;
                        this.inventoryClassName = "basket";
                        this.dialogTitleLangCode = "basketcontents";
                        if (this.type == null)
                        {
                            this.type = "reed";
                        }
                    }
                    this.InitInventory(null);
                    this.LateInitInventory();
                }
            }
            else if (this.type != prevType)
            {
                this.InitInventory(base.Block);
                if (this.Api == null)
                {
                    this.Api = worldForResolving.Api;
                }
                this.LateInitInventory();
                typeChanged = true;
            }
            string oldWoodType = this.woodType;
            this.woodType = tree.GetString("woodType");
            this.metalType = tree.GetString("metalType");
            if (worldForResolving.Api.Side == EnumAppSide.Client)
            {          
                if (tree.GetBool("flagUp") != this.flagUp)
                {
                    this.flagUp = tree.GetBool("flagUp");
                    if (this.animUtil != null)
                    {
                        if (flagUp)
                        {
                            this.animUtil.StartAnimation(new AnimationMetaData
                            {
                                Animation = "flagup",
                                Code = "flagup",
                                AnimationSpeed = 1.8f,
                                EaseOutSpeed = 6f,
                                EaseInSpeed = 15f
                            });
                        }
                        else
                        {
                            this.flagChecked = false;
                            this.animUtil.StopAnimation("flagup");
                        }
                    }
                    
                    

                    //this.animUtil.StopAnimation("lidopen");
                   /* if (this.Api == null)
                    {
                        this.Api = worldForResolving.Api;
                    }
                    this.flagUp = tree.GetBool("flagUp");
                    this.ownMesh = this.GenMesh((worldForResolving.Api as ICoreClientAPI).Tesselator, true);
                    this.MarkDirty(true);*/
                }
                else if(typeChanged)
                {
                    string key = GetMeshKey();
                    this.FillTextureDict();

                    this.ownMesh = animUtil.InitializeAnimator(string.Concat(new string[]
                    {
                            key,
                            "-",
                            flagUp.ToString()
                    }), Vintagestory.API.Common.Shape.TryGet(this.Api, GetShapeString()), this, this.rendererRot);
                    this.animUtil.StartAnimation(new AnimationMetaData
                    {
                        Animation = "flagup",
                        Code = "flagup",
                        AnimationSpeed = 1.8f,
                        EaseOutSpeed = 6f,
                        EaseInSpeed = 15f
                    });
                    this.animUtil.StopAnimation("flagup");
                }
            }
            else
            {
                this.flagUp = tree.GetBool("flagUp");
            }

            /*if (this.Api != null && this.Api.Side == EnumAppSide.Client)
            {
                this.ownMesh = null;
                this.MarkDirty(true, null);
            }*/
            base.FromTreeAttributes(tree, worldForResolving);
        }       
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (base.Block != null)
            {
                tree.SetString("forBlockCode", base.Block.Code.ToShortString());
            }
            if (this.type == null)
            {
                this.type = this.defaultType;
            }
            tree.SetString("type", this.type);
            tree.SetFloat("meshAngle", this.MeshAngle);
            tree.SetString("ownerUID", ownerUID);
            tree.SetString("ownerName", ownerName);
            tree.SetBool("flagUp", this.flagUp);
            tree.SetString("woodType", this.woodType);
            tree.SetString("metalType", this.metalType);
        }
        protected virtual void InitInventory(Block block)
        {
            if (((block != null) ? block.Attributes : null) != null)
            {
                JsonObject jsonObject = block.Attributes["collisionSelectionBoxes"];
                Cuboidf[] array;
                if (jsonObject == null)
                {
                    array = null;
                }
                else
                {
                    JsonObject jsonObject2 = jsonObject[this.type];
                    array = ((jsonObject2 != null) ? jsonObject2.AsObject<Cuboidf[]>(null) : null);
                }
                this.collisionSelectionBoxes = array;
                this.inventoryClassName = block.Attributes["inventoryClassName"].AsString(this.inventoryClassName);
                this.dialogTitleLangCode = block.Attributes["dialogTitleLangCode"][this.type].AsString(this.dialogTitleLangCode);
                this.quantitySlots = block.Attributes["quantitySlots"][this.type].AsInt(this.quantitySlots);
                this.quantityColumns = block.Attributes["quantityColumns"][this.type].AsInt(4);
                this.retrieveOnly = block.Attributes["retrieveOnly"][this.type].AsBool(false);
                if (block.Attributes["typedOpenSound"][this.type].Exists)
                {
                    this.OpenSound = AssetLocation.Create(block.Attributes["typedOpenSound"][this.type].AsString(this.OpenSound.ToShortString()), block.Code.Domain);
                }
                if (block.Attributes["typedCloseSound"][this.type].Exists)
                {
                    this.CloseSound = AssetLocation.Create(block.Attributes["typedCloseSound"][this.type].AsString(this.CloseSound.ToShortString()), block.Code.Domain);
                }
            }
            //this.inventory = new InventoryGeneric(this.quantitySlots, null, null, null);
            if (((block != null) ? block.Attributes : null) != null)
            {
                this.inventoryClassName = Block.Attributes["inventoryClassName"].AsString(this.inventoryClassName);
                this.dialogTitleLangCode = Block.Attributes["dialogTitleLangCode"].AsString(this.dialogTitleLangCode);
                this.quantitySlots = Block.Attributes["quantitySlots"].AsInt(this.quantitySlots);
                this.retrieveOnly = Block.Attributes["retrieveOnly"].AsBool(false);
            }
            
            this.inventory = new InventoryCANMailBox((string)null, (ICoreAPI)null, this.quantitySlots);
            /* this.inventory.BaseWeight = 1f;
             inventory.OnGetSuitability = (sourceSlot, targetSlot, isMerge) => (isMerge ? (inventory.BaseWeight + 3) : (inventory.BaseWeight + 1)) + (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
             this.inventory.OnGetAutoPullFromSlot = new GetAutoPullFromSlotDelegate(this.GetAutoPullFromSlot);
             this.container.Reset();
             if (((block != null) ? block.Attributes : null) != null)
             {
                 if (block.Attributes["spoilSpeedMulByFoodCat"][this.type].Exists)
                 {
                     this.inventory.PerishableFactorByFoodCategory = block.Attributes["spoilSpeedMulByFoodCat"][this.type].AsObject<Dictionary<EnumFoodCategory, float>>(null);
                 }
                 if (block.Attributes["transitionSpeedMul"][this.type].Exists)
                 {
                     this.inventory.TransitionableSpeedMulByType = block.Attributes["transitionSpeedMul"][this.type].AsObject<Dictionary<EnumTransitionType, float>>(null);
                 }
             }*/
            this.inventory.PutLocked = this.retrieveOnly;
            this.inventory.OnInventoryClosed += this.OnInvClosed;
            this.inventory.OnInventoryOpened += this.OnInvOpened;
        }
        public virtual void LateInitInventory()
        {
            InventoryCANMailBox inventoryBase = this.Inventory;
            string str = this.InventoryClassName;
            string str2 = "-";
            BlockPos pos = this.Pos;
            inventoryBase.LateInitialize(str + str2 + ((pos != null) ? pos.ToString() : null), this.Api, this);
            this.Inventory.ResolveBlocksOrItems();
            this.container.LateInit();
            this.MarkDirty(false, null);
        }
        private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            if (atBlockFace == BlockFacing.DOWN)
            {
                return this.inventory.FirstOrDefault((ItemSlot slot) => !slot.Empty);
            }
            return null;
        }
        protected virtual void OnInvOpened(IPlayer player)
        {
            this.inventory.PutLocked = (this.retrieveOnly && player.WorldData.CurrentGameMode != EnumGameMode.Creative);
            if (this.Api.Side == EnumAppSide.Client)
            {
                this.OpenLid();
            }
        }
        public void OpenLid()
        {
            BlockEntityAnimationUtil animUtil = this.animUtil;
            if (animUtil != null && !animUtil.activeAnimationsByAnimCode.ContainsKey("lidopen"))
            {
                BlockEntityAnimationUtil animUtil2 = this.animUtil;
                if (animUtil2 == null)
                {
                    return;
                }
                animUtil2.StartAnimation(new AnimationMetaData
                {
                    Animation = "lidopen",
                    Code = "lidopen",
                    AnimationSpeed = 1.8f,
                    EaseOutSpeed = 6f,
                    EaseInSpeed = 15f
                });
               /* animUtil2.StartAnimation(new AnimationMetaData
                {
                    Animation = "flagup",
                    Code = "flagup",
                    AnimationSpeed = 1.8f,
                    EaseOutSpeed = 6f,
                    EaseInSpeed = 15f
                });*/
            }
        }
        public void CloseLid()
        {
            BlockEntityAnimationUtil animUtil = this.animUtil;
            if (animUtil != null && animUtil.activeAnimationsByAnimCode.ContainsKey("lidopen"))
            {
                BlockEntityAnimationUtil animUtil2 = this.animUtil;
                if (animUtil2 == null)
                {
                    return;
                }
                animUtil2.StopAnimation("lidopen");
                //animUtil2.StopAnimation("flagup");
            }
        }
        protected virtual void OnInvClosed(IPlayer player)
        {
            if (this.LidOpenEntityId.Count == 0 || (this.LidOpenEntityId.Count == 1 && this.LidOpenEntityId.First() == player.Entity.EntityId))
            {
                this.CloseLid();
            }
            this.inventory.PutLocked = this.retrieveOnly;
            GuiDialogBlockEntity inv = this.invDialog;
            this.invDialog = null;
            if (inv != null && inv.IsOpened() && inv != null)
            {
                inv.TryClose();
            }
            if (inv != null)
            {
                inv.Dispose();
            }
            /*if (this.Api.Side == EnumAppSide.Client)
            {
                if (!this.inventory.Empty)
                {
                    if (!this.flagUp)
                    {
                        this.flagUp = true;
                        this.animUtil.StopAnimation("lidopen");
                        this.ownMesh = this.GenMesh((this.Api as ICoreClientAPI).Tesselator, true);
                        this.MarkDirty(true);
                    }
                }
                else
                {
                    if (this.flagUp)
                    {
                        this.flagUp = false;
                        this.animUtil.StopAnimation("lidopen");
                        this.ownMesh = this.GenMesh((this.Api as ICoreClientAPI).Tesselator, true);
                        this.MarkDirty(true);
                    }
                }
            }
            else*/
            if (this.Api.Side == EnumAppSide.Server)
            {
                if (!this.inventory.Empty)
                {
                    if (!this.flagUp)
                    {
                        this.flagUp = true;
                        this.MarkDirty(true, player);
                    }
                }
                else
                {
                    if (this.flagUp)
                    {
                        this.flagUp = false;
                        this.MarkDirty(true, player);
                    }
                }
            }
        }
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            //return base.OnPlayerRightClick(byPlayer, blockSel);
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                this.inventory.PutLocked = false;
            }
            if (this.inventory.PutLocked && this.inventory.Empty)
            {
                return false;
            }
            if (this.Api.World is IServerWorldAccessor)
            {
                byte[] data = BlockEntityContainerOpen.ToBytes("BlockEntityInventory", Lang.Get(this.dialogTitleLangCode, Array.Empty<object>()), (byte)this.quantityColumns, this.inventory);
                //((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos, 5000, data);
                byPlayer.InventoryManager.OpenInventory(this.inventory);
                data = SerializerUtil.Serialize<OpenContainerLidPacket>(new OpenContainerLidPacket(byPlayer.Entity.EntityId, this.LidOpenEntityId.Count > 0));
                /*((ICoreServerAPI)this.Api).Network.BroadcastBlockEntityPacket(this.Pos, 5001, data, new IServerPlayer[]
                {
                     (IServerPlayer)byPlayer
                });*/
            }
            if (Api.Side == EnumAppSide.Client)
            {
                if (invDialog == null)
                {
                    ICoreClientAPI capi = Api as ICoreClientAPI;

                     foreach (var it in byPlayer.InventoryManager.OpenedInventories)
                     {
                         if (it is InventoryCANMailBox)
                         {
                             ((it as InventoryCANMailBox).be as BEMailBox).invDialog?.TryClose();
                             byPlayer.InventoryManager.CloseInventory(it);
                             //(it as InventoryCANStall).be
                             capi.Network.SendBlockEntityPacket((it as InventoryCANMailBox).be.Pos, 1001);
                            // capi.Network.SendPacketClient(it.Close(byPlayer));
                            return false;
                             break;
                         }
                     }
                    if (blockSel.Block is CANBlockGenericTypedContainer)
                    {
                        invDialog = new GuiDialogBlockEntityMailBoxInventory(this.DialogTitle, Inventory, Pos, 0, Api as ICoreClientAPI);
                        Block block = Api.World.BlockAccessor.GetBlock(Pos);
                        string text = block.Attributes?["openSound"]?.AsString();
                        string text2 = block.Attributes?["closeSound"]?.AsString();
                        AssetLocation assetLocation = ((text == null) ? null : AssetLocation.Create(text, block.Code.Domain));
                        AssetLocation assetLocation2 = ((text2 == null) ? null : AssetLocation.Create(text2, block.Code.Domain));
                        invDialog.OpenSound = assetLocation ?? OpenSound;
                        invDialog.CloseSound = assetLocation2 ?? CloseSound;
                        invDialog.TryOpen();
                    }
                    /*invDialog.OnClosed += delegate
                    {
                        invDialog = null;
                        capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1001);
                        capi.Network.SendPacketClient(Inventory.Close(byPlayer));
                    };*/
                   /* invDialog.TryOpen();
                    capi.Network.SendPacketClient(Inventory.Open(byPlayer));
                    capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1000);*/
                }
                else
                {
                    //invDialog.TryClose();
                }
            }
            return true;
        }
        public void FillTextureDict()
        {
            if (this.type == "owl")
            {
                this.tmpAssets["top"] = new AssetLocation("game:block/wood/chest/owl/top.png");
                this.tmpAssets["sides2"] = new AssetLocation("game:block/wood/chest/owl/sides2.png");
                this.tmpAssets["sides1"] = new AssetLocation("game:block/wood/chest/owl/sides1.png");
                this.tmpAssets["inside"] = new AssetLocation("game:block/wood/chest/owl/inside.png");
                this.tmpAssets["inside"] = new AssetLocation("game:block/wood/chest/owl/inside.png");
                this.tmpAssets["copper"] = new AssetLocation("game:block/metal/plate/copper.png");
                this.tmpAssets["aged"] = new AssetLocation("game:block/wood/debarked/aged.png");
                this.tmpAssets["iron"] = new AssetLocation("game:block/metal/plate/iron.png");
                return;
                //"game:block/wood/debarked/aged"
            }
            if (this.type == "wallmounted-typed" && this.woodType != "normal" && this.woodType != "")
            {
                this.tmpAssets["wallmounted"] = new AssetLocation("game:block/metal/plate/iron.png");
                this.tmpAssets["black"] = new AssetLocation("game:block/black.png");
                this.tmpAssets["red"] = new AssetLocation("game:block/clay/hardened/red.png");
                this.tmpAssets["iron"] = new AssetLocation("game:block/metal/sheet/" + this.metalType + "1.png");
                this.tmpAssets["nickel"] = new AssetLocation("game:block/metal/plate/nickel.png");
                this.tmpAssets["iron5"] = new AssetLocation("game:block/metal/sheet/iron5.png");
                this.tmpAssets["molybdochalkos1"] = new AssetLocation("game:block/metal/sheet/" + this.metalType + "1.png");
                this.tmpAssets["inside"] = new AssetLocation("game:block/wood/planks/" + this.woodType + "1.png");
                this.tmpAssets["inner side"] = new AssetLocation("game:block/wood/planks/" + this.woodType + "1.png");
            }
            else if(this.type == "wallmounted" || this.type == "wallmounted-typed" || this.woodType == "normal")
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
        public MeshData GenMesh(ITesselatorAPI tesselator, bool updateAnim = false)
        {
            CANBlockGenericTypedContainer block = base.Block as CANBlockGenericTypedContainer;
            if (base.Block == null)
            {
                block = (this.Api.World.BlockAccessor.GetBlock(this.Pos) as CANBlockGenericTypedContainer);
                base.Block = block;
            }
            if (block == null)
            {
                return null;
            }
            JsonObject attributes = base.Block.Attributes;
            int? num;
            if (attributes == null)
            {
                num = null;
            }
            else
            {
                JsonObject jsonObject = attributes["rndTexNum"][this.type];
                num = ((jsonObject != null) ? new int?(jsonObject.AsInt(0)) : null);
            }
            if(updateAnim)
            {
                //this.animUtil.Dispose();
                //this.animUtil.renderer.Dispose();
                //this.animUtil.renderer = null;
               // this.animUtil = null;
            }
            int? num2 = num;
            int rndTexNum = num2.GetValueOrDefault();
            string key = GetMeshKey();
            Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate<Dictionary<string, MeshData>>(this.Api, key, () => new Dictionary<string, MeshData>());
            JsonObject attributes2 = base.Block.Attributes;
            string shapename = (attributes2 != null) ? attributes2["shape"][this.type].AsString(null) : null;
            FillTextureDict();
            if (shapename == null)
            {
                return null;
            }
            Shape shape = null;
            if (this.animUtil != null)
            {
                string skeydict = "typedContainerShapes";
                Dictionary<string, Shape> shapes = ObjectCacheUtil.GetOrCreate<Dictionary<string, Shape>>(this.Api, skeydict, () => new Dictionary<string, Shape>());
                string skey = string.Concat(new string[]
                {
                    base.Block.FirstCodePart(0),
                    this.woodType,
                    this.type,
                    block.Subtype,
                    "--",
                    shapename
                });
                if (!shapes.TryGetValue(skey, out shape))
                {
                    shape = (shapes[skey] = block.GetShape(this.Api as ICoreClientAPI, shapename));
                }
            }
            string meshKey = this.type + block.Subtype + this.flagUp;
            MeshData mesh = null;
            if (meshes.TryGetValue(meshKey, out mesh))
            {
                if ((this.animUtil != null && this.animUtil.renderer == null) || updateAnim)
                {
                    FillTextureDict();
                    mesh = this.animUtil.InitializeAnimator(string.Concat(new string[]
                    {
                        key,
                        "-",
                        flagUp.ToString()
                    }),Vintagestory.API.Common.Shape.TryGet(this.Api, GetShapeString()), this, this.rendererRot);
                }
                return mesh;
            }
            if (rndTexNum > 0)
            {
                rndTexNum = GameMath.MurmurHash3Mod(this.Pos.X, this.Pos.Y, this.Pos.Z, rndTexNum);
            }
            if (this.animUtil != null)
            {
                /*if(updateAnim)
                {
                    this.animUtil.renderer.Dispose();
                    this.animUtil.renderer = null;
                }*/
                if (this.animUtil.renderer == null || updateAnim)
                {
                    FillTextureDict();
                    mesh = this.animUtil.InitializeAnimator(string.Concat(new string[]
                    {
                        key,
                        "-",
                        flagUp.ToString()
                    }), Vintagestory.API.Common.Shape.TryGet(this.Api, GetShapeString()), this, this.rendererRot);
                }
                this.ownMesh = mesh;
                return meshes[meshKey] = mesh;
            }
            mesh = block.GenMesh(this.Api as ICoreClientAPI, this.type, shapename, tesselator, new Vec3f(), rndTexNum);
            this.ownMesh = mesh;
            return meshes[meshKey] = mesh;
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (!base.OnTesselation(mesher, tesselator))
            {
                //ownMesh = null;
                if (this.ownMesh == null)
                {
                    this.ownMesh = this.GenMesh(tesselator);
                    if (this.ownMesh == null)
                    {
                        return false;
                    }
                }
                mesher.AddMeshData(this.ownMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, this.MeshAngle, 0f), 1);
            }
            return true;
        }
        public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
        {
            this.MeshAngle = tree.GetFloat("meshAngle", 0f);
            this.MeshAngle -= (float)degreeRotation * 0.017453292f;
            tree.SetFloat("meshAngle", this.MeshAngle);
        }
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if(packetid == 2025)
            {
                inventory.PutLocked = true;
                inventory.TakeLocked = true;
                for (int j = 0; j < 2; j++) {
                    if (!inventory[j].Empty)
                    {
                        for (int i = 2; i < this.inventory.Count; i++)
                        {
                            if (inventory.Slots[i].Empty)
                            {
                                inventory[i].Itemstack = inventory[j].Itemstack;
                                inventory[j].Itemstack = null;
                                inventory[j].MarkDirty();
                                inventory[i].MarkDirty();
                                this.MarkDirty();
                            }
                        }
                    }
                }
                inventory.PutLocked = false;
                inventory.TakeLocked = false;
                return;
            }
            if (packetid < 1000)
            {
                Packet_Client f = Packet_ClientSerializer.DeserializeBuffer(data, data.Length, new Packet_Client());
                if (player.PlayerUID.Equals(ownerUID) || f?.ActivateInventorySlot?.TargetSlot < 2)
                {
                    Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                    Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
                }
                return;
            }

            if (packetid == 1001)
            {
                player.InventoryManager?.CloseInventory(Inventory);
                data = SerializerUtil.Serialize(new OpenContainerLidPacket(player.Entity.EntityId, opened: false));
                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 5001, data, (IServerPlayer)player);
            }

            if (packetid == 1000)
            {
                player.InventoryManager?.OpenInventory(Inventory);
                data = SerializerUtil.Serialize(new OpenContainerLidPacket(player.Entity.EntityId, opened: true));
                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 5001, data, (IServerPlayer)player);
            }
        }
        public string GetMeshKey()
        {
            return "typedContainerMeshes" + this.rendererRot + this.type + this.woodType + this.metalType;
        }
    }
}
