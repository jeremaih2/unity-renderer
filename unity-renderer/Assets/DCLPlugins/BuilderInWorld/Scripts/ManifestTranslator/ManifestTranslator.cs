using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DCL.Builder.Manifest;
using DCL.Components;
using DCL.Configuration;
using DCL.Controllers;
using DCL.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DCL.Builder
{
    public static class ManifestTranslator
    {
        private static readonly Dictionary<string, int> idToHumanReadableDictionary = new Dictionary<string, int>()
        {
            { "Transform", (int) CLASS_ID_COMPONENT.TRANSFORM },

            { "GLTFShape", (int) CLASS_ID.GLTF_SHAPE },

            { "NFTShape", (int) CLASS_ID.NFT_SHAPE },

            { "Name", (int)CLASS_ID.NAME },

            { "LockedOnEdit", (int)CLASS_ID.LOCKED_ON_EDIT },

            { "VisibleOnEdit", (int)CLASS_ID.VISIBLE_ON_EDIT },

            { "Script", (int) CLASS_ID_COMPONENT.SMART_ITEM }
        };

        public static WebBuilderScene StatelessToWebBuilderScene(StatelessManifest manifest, Vector2Int parcelSize)
        {
            WebBuilderScene builderScene = new WebBuilderScene();
            builderScene.id = Guid.NewGuid().ToString();

            BuilderGround ground = new BuilderGround();
            List<string> namesList = new List<string>();
            //We iterate all the entities to create its counterpart in the builder manifest
            //我们迭代所有实体以在构建器清单中创建对应的实体
            foreach (Entity entity in manifest.entities)
            {
                BuilderEntity builderEntity = new BuilderEntity();
                builderEntity.id = entity.id;
                string entityName = builderEntity.id;

                // Iterate the entity components to transform them to the builder format
                //迭代实体组件，将它们转换为构建器格式
                foreach (Component entityComponent in entity.components)
                {
                    BuilderComponent builderComponent = new BuilderComponent();
                    builderComponent.id = Guid.NewGuid().ToString();
                    builderComponent.type = entityComponent.type;
                    builderComponent.data = entityComponent.value;
                    builderEntity.components.Add(builderComponent.id);

                    if (entityComponent.type == "NFTShape")
                    {
                        NFTShape.Model model = JsonConvert.DeserializeObject<NFTShape.Model>(entityComponent.value.ToString());
                        builderComponent.data = JsonConvert.SerializeObject(GetWebBuilderRepresentationOfNFT(model));

                        //This is the name format that is used by builder, we will have a different name in unity due to DCLName component
                        //这是由生成器使用的名称格式，我们将有一个不同的名称在统一由于DCLName组件
                        entityName = "nft";
                    }

                    if (entityComponent.type ==  "GLTFShape")
                    {
                        var gltfModel = JsonConvert.DeserializeObject<GLTFShape.Model>(builderComponent.data.ToString());

                        //We get the associated asset to the GLFTShape and add it to the scene 
                        //我们将相关资产添加到GLFTShape，并将其添加到场景中
                        var asset = AssetCatalogBridge.i.sceneObjectCatalog.Get(gltfModel.assetId);
                        if (!builderScene.assets.ContainsKey(asset.id))
                            builderScene.assets.Add(asset.id, asset);

                        //If the asset is a floor, we handle this situation for builder 
                        //如果资产是地板，我们为构建者处理这种情况
                        if (asset.category == BIWSettings.FLOOR_CATEGORY)
                        {
                            ground.assetId = asset.id;
                            ground.componentId = builderComponent.id;
                            builderEntity.disableGizmos = true;
                        }

                        entityName = asset.name;
                    }

                    if (!builderScene.components.ContainsKey(builderComponent.id))
                        builderScene.components.Add(builderComponent.id, builderComponent);
                }

                // We need to give to each entity a unique name so we search for a unique name there
                //我们需要给每个实体一个唯一的名称，以便在那里搜索一个唯一的名称
                // Also, since the name of the entity will be used in the code, we need to ensure that the it doesn't have special characters or spaces
                //此外，由于实体的名称将在代码中使用，我们需要确保它没有特殊字符或空格
                builderEntity.name = GetCleanUniqueName(namesList, entityName);

                if (!builderScene.entities.ContainsKey(builderEntity.id))
                    builderScene.entities.Add(builderEntity.id, builderEntity);
            }

            //We add the limits to the scene, the current metrics are calculated in the builder
            //我们将限制添加到场景中，当前的指标在构建器中计算
            builderScene.limits = BIWUtils.GetSceneMetricsLimits(parcelSize.x + parcelSize.y);
            builderScene.ground = ground;

            return builderScene;
        }

        public static StatelessManifest WebBuilderSceneToStatelessManifest(WebBuilderScene scene)
        {
            StatelessManifest manifest = new StatelessManifest();
            manifest.schemaVersion = 1;
            
            foreach (var entity in scene.entities.Values)
            {
                Entity statlesEntity = new Entity();
                statlesEntity.id = entity.id;

                foreach (string componentId in entity.components)
                {
                    foreach (BuilderComponent component in scene.components.Values)
                    {
                        if(component.id != componentId)
                            continue;
                        
                        Component statelesComponent = new Component();
                        statelesComponent.type = component.type;

                        if (statelesComponent.type == "NFTShape")
                        {
                            string url;
                            try
                            {
                                // Builder use a different way to load the NFT so we convert it to our system
                                //Builder使用不同的方式来加载NFT，这样我们就可以将它转换到我们的系统中
                                url = ((NFTShapeBuilderRepresentantion) component.data).url;
                            }
                            catch (Exception e)
                            {
                                // Builder handles the components differently if they come from another site, if we can't do it correctly, we go this way
                                //如果组件来自其他站点，Builder会以不同的方式处理它们，如果我们不能正确地处理，我们就会这样做
                                JObject jObject = JObject.Parse(component.data.ToString());
                                url = jObject["url"].ToString();
                            }
                            string assedId = url.Replace(BIWSettings.NFT_ETHEREUM_PROTOCOL, "");
                            int index = assedId.IndexOf("/", StringComparison.Ordinal);
                            string partToremove = assedId.Substring(index);
                            assedId = assedId.Replace(partToremove, "");

                            // We need to use this kind of representation because the color from unity is not serializable to SDK standard
                            //我们需要使用这种表示，因为unity的颜色不能序列化到SDK标准
                            NFTShapeStatelessRepresentantion nftModel = new NFTShapeStatelessRepresentantion();
                            nftModel.color = new NFTShapeStatelessRepresentantion.ColorRepresentantion(0.6404918f, 0.611472f, 0.8584906f);
                            nftModel.src = url;
                            nftModel.assetId = assedId;
                            
                            statelesComponent.value = nftModel;
                        }
                        else
                        {
                            statelesComponent.value = component.data;
                        }

                        statlesEntity.components.Add(statelesComponent);
                    }
                }

                manifest.entities.Add(statlesEntity);
            }

            return manifest;
        }
        
        public static StatelessManifest ParcelSceneToStatelessManifest(IParcelScene scene)
        {
            StatelessManifest manifest = new StatelessManifest();
            manifest.schemaVersion = 1;

            foreach (var entity in scene.entities.Values)
            {
                Entity statlesEntity = new Entity();
                statlesEntity.id = entity.entityId.ToString();

                foreach (KeyValuePair<CLASS_ID_COMPONENT, IEntityComponent> entityComponent in scene.componentsManagerLegacy.GetComponentsDictionary(entity))
                {
                    Component statelesComponent = new Component();
                    statelesComponent.type = idToHumanReadableDictionary.FirstOrDefault( x => x.Value == (int)entityComponent.Key).Key;

                    // Transform component is handle a bit different due to quaternion serializations
                    //由于四元数序列化，转换组件的处理略有不同
                    if (entityComponent.Key == CLASS_ID_COMPONENT.TRANSFORM)
                    {
                        ProtocolV2.TransformComponent entityTransformComponentModel = new ProtocolV2.TransformComponent();
                        entityTransformComponentModel.position = WorldStateUtils.ConvertUnityToScenePosition(entity.gameObject.transform.position, scene);
                        entityTransformComponentModel.rotation = new ProtocolV2.QuaternionRepresentation(entity.gameObject.transform.rotation);
                        entityTransformComponentModel.scale = entity.gameObject.transform.lossyScale;

                        statelesComponent.value = entityTransformComponentModel;
                    }
                    else
                    {
                        statelesComponent.value = entityComponent.Value.GetModel();
                    }

                    statlesEntity.components.Add(statelesComponent);
                }

                foreach (KeyValuePair<Type, ISharedComponent> entitySharedComponent in scene.componentsManagerLegacy.GetSharedComponentsDictionary(entity))
                {
                    Component statelesComponent = new Component();
                    statelesComponent.type = idToHumanReadableDictionary.FirstOrDefault( x => x.Value == (int)entitySharedComponent.Value.GetClassId()).Key;
                    statelesComponent.value = entitySharedComponent.Value.GetModel();
                    statlesEntity.components.Add(statelesComponent);
                }

                manifest.entities.Add(statlesEntity);
            }

            return manifest;
        }

        public static WebBuilderScene ParcelSceneToWebBuilderScene(ParcelScene scene)
        {
            WebBuilderScene builderScene = new WebBuilderScene();
            builderScene.id = Guid.NewGuid().ToString();

            BuilderGround ground = new BuilderGround();
            List<string> namesList = new List<string>();

            //We iterate all the entities to create its counterpart in the builder manifest
            //我们迭代所有实体以在构建器清单中创建对应的实体
            foreach (IDCLEntity entity in scene.entities.Values)
            {
                BuilderEntity builderEntity = new BuilderEntity();
                builderEntity.id = entity.entityId.ToString();
                string componentType = "";
                string entityName = "";

                // Iterate the entity components to transform them to the builder format
                //迭代实体组件，将它们转换为构建器格式
                foreach (KeyValuePair<CLASS_ID_COMPONENT, IEntityComponent> entityComponent in scene.componentsManagerLegacy.GetComponentsDictionary(entity))
                {
                    BuilderComponent builderComponent = new BuilderComponent();
                    switch (entityComponent.Key)
                    {
                        case CLASS_ID_COMPONENT.TRANSFORM:
                            componentType = "Transform";

                            // We can't serialize the quaternions from Unity since newton serializes have recursive problems so we add this model
                            //我们不能从Unity序列化四元数，因为牛顿序列化有递归问题，所以我们添加了这个模型
                            ProtocolV2.TransformComponent entityTransformComponentModel = new ProtocolV2.TransformComponent();
                            entityTransformComponentModel.position = WorldStateUtils.ConvertUnityToScenePosition(entity.gameObject.transform.position, scene);
                            entityTransformComponentModel.rotation = new ProtocolV2.QuaternionRepresentation(entity.gameObject.transform.rotation);
                            entityTransformComponentModel.scale = entity.gameObject.transform.lossyScale;

                            builderComponent.data = entityTransformComponentModel;

                            break;
                        case CLASS_ID_COMPONENT.SMART_ITEM:
                            componentType = "Script";
                            break;
                    }

                    // We generate a new uuid for the component since there is no uuid for components in the stateful scheme
                    //我们为组件生成一个新的uuid，因为在有状态方案中组件没有uuid
                    builderComponent.id = Guid.NewGuid().ToString();
                    builderComponent.type = componentType;

                    // Since the transform model data is different from the others, we set it in the switch instead of here
                    //由于转换模型数据不同于其他数据，所以我们将其设置在开关中，而不是这里
                    if (builderComponent.type != "Transform")
                        builderComponent.data = entityComponent.Value.GetModel();

                    builderEntity.components.Add(builderComponent.id);

                    if (!builderScene.components.ContainsKey(builderComponent.id))
                        builderScene.components.Add(builderComponent.id, builderComponent);
                }

                // Iterate the entity shared components to transform them to the builder format
                //迭代实体共享组件，将它们转换为构建器格式
                foreach (KeyValuePair<System.Type, ISharedComponent> sharedEntityComponent in scene.componentsManagerLegacy.GetSharedComponentsDictionary(entity))
                {
                    BuilderComponent builderComponent = new BuilderComponent();
                    // We generate a new uuid for the component since there is no uuid for components in the stateful scheme
                    builderComponent.id = Guid.NewGuid().ToString();
                    builderComponent.data = sharedEntityComponent.Value.GetModel();

                    if (sharedEntityComponent.Value is GLTFShape)
                    {
                        componentType = "GLTFShape";
                        
                        var gltfModel = (GLTFShape.Model) builderComponent.data;

                        //We get the associated asset to the GLFTShape and add it to the scene 
                        //我们将相关资产添加到GLFTShape，并将其添加到场景中
                        var asset = AssetCatalogBridge.i.sceneObjectCatalog.Get(gltfModel.assetId);
                        if (!builderScene.assets.ContainsKey(asset.id))
                            builderScene.assets.Add(asset.id, asset);

                        // This is a special case. The builder needs the ground separated from the rest of the components so we search for it.
                        //这是一个特例。建造者需要地面与其他组件分开，所以我们搜索它。
                        // Since all the grounds have the same asset, we assign it and disable the gizmos in the builder.我们给它赋值并在构建器中禁用gizmos
                        if (asset.category == BIWSettings.FLOOR_CATEGORY)
                        {
                            ground.assetId = asset.id;
                            ground.componentId = builderComponent.id;
                            builderEntity.disableGizmos = true;
                        }

                        entityName = asset.name;
                    }
                    else if (sharedEntityComponent.Value is NFTShape)
                    {
                        componentType = "NFTShape";

                        // This is a special case where we are assigning the builder url field for NFTs because builder model data is different
                        //这是一个特殊的情况，我们为nft分配生成器url字段，因为生成器模型数据是不同的
                        NFTShape.Model model = (NFTShape.Model) builderComponent.data;
                        builderComponent.data = GetWebBuilderRepresentationOfNFT(model);

                        //This is the name format that is used by builder, we will have a different name in unity due to DCLName component
                        //这是由生成器使用的名称格式，我们将有一个不同的名称在统一由于DCLName组件
                        entityName = "nft";
                    }
                    else if (sharedEntityComponent.Key == typeof(DCLName))
                    {
                        componentType = "Name";
                        entityName = ((DCLName.Model) sharedEntityComponent.Value.GetModel()).value;
                    }
                    else if (sharedEntityComponent.Key == typeof(DCLLockedOnEdit))
                    {
                        componentType = "LockedOnEdit";
                    }

                    builderComponent.type = componentType;

                    builderEntity.components.Add(builderComponent.id);
                    if (!builderScene.components.ContainsKey(builderComponent.id))
                        builderScene.components.Add(builderComponent.id, builderComponent);
                }

                // We need to give to each entity a unique name so we search for a unique name there
                //我们需要给每个实体一个唯一的名称，以便在那里搜索一个唯一的名称
                // Also, since the name of the entity will be used in the code, we need to ensure that the it doesn't have special characters or spaces
                //此外，由于实体的名称将在代码中使用，我们需要确保它没有特殊字符或空格
                builderEntity.name = GetCleanUniqueName(namesList, entityName);

                if (!builderScene.entities.ContainsKey(builderEntity.id))
                    builderScene.entities.Add(builderEntity.id, builderEntity);
            }

            //We add the limits to the scene, the current metrics are calculated in the builder
            //我们将限制添加到场景中，当前的指标在构建器中计算
            builderScene.limits = BIWUtils.GetSceneMetricsLimits(scene.parcels.Count);
            builderScene.metrics = new SceneMetricsModel();
            builderScene.ground = ground;

            return builderScene;
        }

        private static NFTShapeBuilderRepresentantion GetWebBuilderRepresentationOfNFT(NFTShape.Model model)
        {
            NFTShapeBuilderRepresentantion representantion = new NFTShapeBuilderRepresentantion();
            representantion.url = model.src;
            return representantion;
        }

        private static string GetCleanUniqueName(List<string> namesList, string currentName)
        {
            //We clean the name to don't include special characters
            //我们清除名称，使其不包含特殊字符
            Regex r = new Regex("(?:[^a-z]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

            string newName = r.Replace(currentName, String.Empty);
            newName = newName.ToLower();

            //We get a unique name我们获取一个唯一名称
            bool uniqueName = false;
            int cont = 2;
            while (!uniqueName)
            {
                if (!namesList.Contains(newName))
                    uniqueName = true;
                else
                    newName = newName + cont;

                cont++;
            }

            namesList.Add(newName);
            return newName;
        }

        public static IParcelScene ManifestToParcelSceneWithOnlyData(Manifest.Manifest manifest)
        {
            GameObject parcelGameObject = new GameObject("Builder Scene SceneId: " + manifest.scene.id);
            ParcelScene scene = parcelGameObject.AddComponent<ParcelScene>();

            //We remove the old assets to they don't collide with the new ones
            //我们移除旧资产，使它们不会与新资产发生冲突
            BIWUtils.RemoveAssetsFromCurrentScene();

            //The data is built so we set the data of the scene 
            //建立好数据后，我们就可以设置场景的数据
            scene.SetData(CreateSceneDataFromScene(manifest));

            return scene;
        }

        private static LoadParcelScenesMessage.UnityParcelScene CreateSceneDataFromScene(Manifest.Manifest manifest)
        {
            //We add the assets from the scene to the catalog 我们将场景中的资产添加到目录中
            var assets = manifest.scene.assets.Values.ToArray();
            AssetCatalogBridge.i.AddScenesObjectToSceneCatalog(assets);

            //We create and assign the data of the scene  我们创建并分配场景的数据
            LoadParcelScenesMessage.UnityParcelScene parcelData = new LoadParcelScenesMessage.UnityParcelScene();
            parcelData.id = manifest.scene.id;

            //We set the current scene in the 0,0 我们设置当前的场景在0，0
            int x = 0;
            int y = 0;

            parcelData.basePosition =  new Vector2Int(x, y);

            //We set the parcels as the first one is in the base position, the first one will be in the bottom-left corner 
            //我们将parcels设置为第一个在基地位置，第一个将在左下角
            parcelData.parcels =  new Vector2Int[manifest.project.rows * manifest.project.cols];

            //We assign the parcels position我们分配parcels位置
            for (int index = 0; index < parcelData.parcels.Length; index++)
            {
                parcelData.parcels[index] = new Vector2Int(x, y);
                x++;
                if (x == manifest.project.rows)
                {
                    y++;
                    x = 0;
                }
            }

            //We prepare the mappings to the scenes 我们准备到场景的映射
            Dictionary<string, string> contentDictionary = new Dictionary<string, string>();

            foreach (var sceneObject in assets)
            {
                foreach (var content in sceneObject.contents)
                {
                    if (!contentDictionary.ContainsKey(content.Key))
                        contentDictionary.Add(content.Key, content.Value);
                }
            }

            //We add the mappings to the scene 我们将映射添加到场景中
            BIWUtils.AddSceneMappings(contentDictionary, BIWUrlUtils.GetUrlSceneObjectContent(), parcelData);

            return parcelData;
        }
        
        public static IParcelScene ManifestToParcelScene(Manifest.Manifest manifest)
        {
            GameObject parcelGameObject = new GameObject("Builder Scene SceneId: " + manifest.scene.id);
            ParcelScene scene = parcelGameObject.AddComponent<ParcelScene>();

            //We remove the old assets to they don't collide with the new ones
            //我们移除旧资产，使它们不会与新资产发生冲突
            BIWUtils.RemoveAssetsFromCurrentScene();

            //The data is built so we set the data of the scene 
            //建立好数据后，我们就可以设置场景的数据
            scene.SetData(CreateSceneDataFromScene(manifest));

            // We iterate all the entities to create the entity in the scene
            //我们迭代所有实体以在场景中创建实体
            foreach (BuilderEntity builderEntity in manifest.scene.entities.Values)
            {
                var entity = scene.CreateEntity(builderEntity.id.GetHashCode());

                bool nameComponentFound = false;
                // We iterate all the id of components in the entity, to add the component 
                //我们迭代实体中所有组件的id，以添加组件
                foreach (string idComponent in builderEntity.components)
                {
                    //This shouldn't happen, the component should be always in the scene, but just in case
                    //这是不应该发生的，组件应该一直在场景中，但只是以防万一
                    if (!manifest.scene.components.ContainsKey(idComponent))
                        continue;

                    // We get the component from the scene and create it in the entity
                    //我们从场景中获取组件，并在实体中创建它
                    BuilderComponent component = manifest.scene.components[idComponent];

                    switch (component.type)
                    {
                        case "Transform":
                            DCLTransform.Model model = JsonConvert.DeserializeObject<DCLTransform.Model>(component.data.ToString());
                            EntityComponentsUtils.AddTransformComponent(scene, entity, model);
                            break;

                        case "GLTFShape":
                            LoadableShape.Model gltfModel = JsonConvert.DeserializeObject<LoadableShape.Model>(component.data.ToString());
                            EntityComponentsUtils.AddGLTFComponent(scene, entity, gltfModel, component.id);
                            break;

                        case "NFTShape":
                            //Builder use a different way to load the NFT so we convert it to our system
                            //Builder使用不同的方式来加载NFT，这样我们就可以将它转换到我们的系统中
                            string url = JsonConvert.DeserializeObject<string>(component.data.ToString());
                            string assedId = url.Replace(BIWSettings.NFT_ETHEREUM_PROTOCOL, "");
                            int index = assedId.IndexOf("/", StringComparison.Ordinal);
                            string partToremove = assedId.Substring(index);
                            assedId = assedId.Replace(partToremove, "");

                            NFTShape.Model nftModel = new NFTShape.Model();
                            nftModel.color = new Color(0.6404918f, 0.611472f, 0.8584906f);
                            nftModel.src = url;
                            nftModel.assetId = assedId;

                            EntityComponentsUtils.AddNFTShapeComponent(scene, entity, nftModel, component.id);
                            break;

                        case "Name":
                            nameComponentFound = true;
                            DCLName.Model nameModel = JsonConvert.DeserializeObject<DCLName.Model>(component.data.ToString());
                            nameModel.builderValue = builderEntity.name;
                            EntityComponentsUtils.AddNameComponent(scene , entity, nameModel, Guid.NewGuid().ToString());
                            break;

                        case "LockedOnEdit":
                            DCLLockedOnEdit.Model lockedModel = JsonConvert.DeserializeObject<DCLLockedOnEdit.Model>(component.data.ToString());
                            EntityComponentsUtils.AddLockedOnEditComponent(scene , entity, lockedModel, Guid.NewGuid().ToString());
                            break;
                    }
                }

                // We need to mantain the builder name of the entity, so we create the equivalent part in biw. We do this so we can maintain the smart-item references
                //我们需要维护实体的构建器名称，因此我们在biw中创建了等价的部分。这样做是为了维护智能条目引用
                if (!nameComponentFound)
                {
                    DCLName.Model nameModel = new DCLName.Model();
                    nameModel.value = builderEntity.name;
                    nameModel.builderValue = builderEntity.name;
                    EntityComponentsUtils.AddNameComponent(scene , entity, nameModel, Guid.NewGuid().ToString());
                }
            }

            //We already have made all the necessary steps to configure the parcel, so we init the scene
            //我们已经完成了配置包的所有必要步骤，因此我们初始化场景
            scene.sceneLifecycleHandler.SetInitMessagesDone();

            return scene;
        }

    }
}