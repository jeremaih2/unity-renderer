using System;
using System.Collections.Generic;
using System.Linq;
using DCL;
using DCL.Emotes;
using UnityEngine;

[Serializable]
public class WearableItem
{//第三方收藏路径
    private const string THIRD_PARTY_COLLECTIONS_PATH = "collections-thirdparty";

    [Serializable]
    public class MappingPair
    {
        public string key;
        public string hash;
    }

    [Serializable]
    public class Representation
    {
        public string[] bodyShapes;
        public string mainFile;
        public MappingPair[] contents;
        public string[] overrideHides;
        public string[] overrideReplaces;
    }

    [Serializable]
    public class Data
    {
        public Representation[] representations;
        public string category;
        public string[] tags;
        public string[] replaces;
        public string[] hides;
        public bool loop;
    }

    public Data data;
    public EmoteDataV0 emoteDataV0;
    public string id;

    public string baseUrl;
    public string baseUrlBundles;

    public i18n[] i18n;
    public string thumbnail;
    
    private string thirdPartyCollectionId;
    public string ThirdPartyCollectionId
    {
        get
        {
            if (!string.IsNullOrEmpty(thirdPartyCollectionId)) return thirdPartyCollectionId;
            if (!id.Contains(THIRD_PARTY_COLLECTIONS_PATH)) return "";
            var paths = id.Split(':');
            var thirdPartyIndex = Array.IndexOf(paths, THIRD_PARTY_COLLECTIONS_PATH);
            thirdPartyCollectionId = string.Join(":", paths, 0, thirdPartyIndex + 2);
            return thirdPartyCollectionId;
        }
    }

    public bool IsFromThirdPartyCollection => !string.IsNullOrEmpty(ThirdPartyCollectionId);
    //缩略精灵图
    public Sprite thumbnailSprite;

    //This fields are temporary, once Kernel is finished we must move them to wherever they are placed//这些字段是临时的，一旦内核完成，我们必须将它们移动到放置它们的地方 
    public string rarity;
    public string description;
    public int issuedId;

    private readonly Dictionary<string, string> cachedI18n = new Dictionary<string, string>();
    private readonly Dictionary<string, ContentProvider> cachedContentProviers =
        new Dictionary<string, ContentProvider>();
//皮肤类别
    private readonly string[] skinImplicitCategories =
    {
        WearableLiterals.Categories.EYES,
        WearableLiterals.Categories.MOUTH,
        WearableLiterals.Categories.EYEBROWS,
        WearableLiterals.Categories.HAIR,
        WearableLiterals.Categories.UPPER_BODY,
        WearableLiterals.Categories.LOWER_BODY,
        WearableLiterals.Categories.FEET,
        WearableLiterals.Misc.HEAD,
        WearableLiterals.Categories.FACIAL_HAIR
    };
//是否尝试获取画像
    public bool TryGetRepresentation(string bodyshapeId, out Representation representation)
    {
        representation = GetRepresentation(bodyshapeId);
        return representation != null;
    }
//获取画像
    public Representation GetRepresentation(string bodyShapeType)
    {
        if (data?.representations == null)
            return null;

        for (int i = 0; i < data.representations.Length; i++)
        {
            if (data.representations[i].bodyShapes.Contains(bodyShapeType))
            {
                return data.representations[i];
            }
        }

        return null;
    }
//获取内容提供者
    public ContentProvider GetContentProvider(string bodyShapeType)
    {
        var representation = GetRepresentation(bodyShapeType);

        if (representation == null)
            return null;

        if (!cachedContentProviers.ContainsKey(bodyShapeType))
        {
            var contentProvider = CreateContentProvider(baseUrl, representation.contents);
            contentProvider.BakeHashes();
            cachedContentProviers.Add(bodyShapeType, contentProvider);
        }

        return cachedContentProviers[bodyShapeType];
    }
//内容创建提供者
    protected virtual ContentProvider CreateContentProvider(string baseUrl, MappingPair[] contents)
    {
        return new ContentProvider
        {
            baseUrl = baseUrl,
            contents = contents.Select(mapping => new ContentServerUtils.MappingPair()
                                   { file = mapping.key, hash = mapping.hash })
                               .ToList()
        };
    }
//是否支持体型
    public bool SupportsBodyShape(string bodyShapeType)
    {
        if (data?.representations == null)
            return false;

        for (int i = 0; i < data.representations.Length; i++)
        {
            if (data.representations[i].bodyShapes.Contains(bodyShapeType))
            {
                return true;
            }
        }

        return false;
    }
//获取替换列表
    public string[] GetReplacesList(string bodyShapeType)
    {
        var representation = GetRepresentation(bodyShapeType);

        if (representation?.overrideReplaces == null || representation.overrideReplaces.Length == 0)
            return data.replaces;

        return representation.overrideReplaces;
    }
//获取隐藏列表
    public string[] GetHidesList(string bodyShapeType)
    {
        var representation = GetRepresentation(bodyShapeType);

        string[] hides;

        if (representation?.overrideHides == null || representation.overrideHides.Length == 0)
            hides = data.hides;
        else
            hides = representation.overrideHides;

        if (IsSkin())
        {
            hides = hides == null
                ? skinImplicitCategories
                : hides.Concat(skinImplicitCategories).Distinct().ToArray();
        }

        return hides;
    }
//清理隐藏列表
    public void SanitizeHidesLists()
    {
        //remove bodyshape from hides list 从隐藏列表中移除形体
        if (data.hides != null)
            data.hides = data.hides.Except(new [] { WearableLiterals.Categories.BODY_SHAPE }).ToArray();
        for (int i = 0; i < data.representations.Length; i++)
        {
            Representation representation = data.representations[i];
            if (representation.overrideHides != null)
                representation.overrideHides = representation.overrideHides.Except(new [] { WearableLiterals.Categories.BODY_SHAPE }).ToArray();

        }
    }
//是否隐藏
    public bool DoesHide(string category, string bodyShape) => GetHidesList(bodyShape).Any(s => s == category);
    //是否是收藏品
    public bool IsCollectible()
    {
        if (id == null)
            return false;

        return !id.StartsWith("urn:decentraland:off-chain:base-avatars:");
    }
    //是否皮肤
    public bool IsSkin() => data.category == WearableLiterals.Categories.SKIN;

    public bool IsSmart()
    {
        if (data?.representations == null) return false;

        for (var i = 0; i < data.representations.Length; i++)
        {
            var representation = data.representations[i];
            var containsGameJs = representation.contents?.Any(pair => pair.key.EndsWith("game.js")) ?? false;
            if (containsGameJs) return true;
        }

        return false;
    }

    public string GetName(string langCode = "en")
    {
        if (!cachedI18n.ContainsKey(langCode))
        {
            cachedI18n.Add(langCode, i18n.FirstOrDefault(x => x.code == langCode)?.text);
        }

        return cachedI18n[langCode];
    }
//返回int的最大值，从稀缺性获得发行数量
    public int GetIssuedCountFromRarity(string rarity)
    {
        switch (rarity)
        {
            case WearableLiterals.ItemRarity.RARE:
                return 5000;
            case WearableLiterals.ItemRarity.EPIC:
                return 1000;
            case WearableLiterals.ItemRarity.LEGENDARY:
                return 100;
            case WearableLiterals.ItemRarity.MYTHIC:
                return 10;
            case WearableLiterals.ItemRarity.UNIQUE:
                return 1;
        }

        return int.MaxValue;
    }
//组成缩略图Url
    public string ComposeThumbnailUrl() { return baseUrl + thumbnail; }
//组成隐藏分类
    public static HashSet<string> ComposeHiddenCategories(string bodyShapeId, List<WearableItem> wearables)
    {
        HashSet<string> result = new HashSet<string>();
        //Last wearable added has priority over the rest最后一个可穿戴的添加优先于其他
        for (int index = 0; index < wearables.Count; index++)
        {
            WearableItem wearableItem = wearables[index];
            if (result.Contains(wearableItem.data.category)) //Skip hidden elements to avoid two elements hiding each other跳过隐藏元素以避免两个元素相互隐藏 
                continue;

            string[] wearableHidesList = wearableItem.GetHidesList(bodyShapeId);
            if (wearableHidesList != null)
            {
                result.UnionWith(wearableHidesList);
            }
        }

        return result;
    }

    //Workaround to know the net of a wearable. 
    //Once wearables are allowed to be moved from Ethereum to Polygon this method wont be reliable anymore
    //To retrieve this properly first we need the catalyst to send the net of each wearable, not just the ID
    //了解可穿戴设备网络的变通方法。
    //一旦可穿戴设备被允许从以太坊转移到Polygon，这种方法就不再可靠了 
    //为了正确地获取它，首先我们需要catalyst来发送每个可穿戴设备的网络，而不仅仅是ID 
    public bool IsInL2()
    {
        if (id.StartsWith("urn:decentraland:matic") || id.StartsWith("urn:decentraland:mumbai"))
            return true;
        return false;
    }

    public bool IsEmote() { return emoteDataV0 != null; }

    public override string ToString() { return id; }
}



[Serializable]
public class EmoteItem : WearableItem
{
}

[Serializable]
public class WearablesRequestResponse
{
    public WearableItem[] wearables;
    public string context;
}

[Serializable]
public class WearablesRequestFailed
{
    public string error;
    public string context;
}

[Serializable]
public class WearableContent
{
    public string file;
    public string hash;
}

[Serializable]
public class i18n
{
    public string code;
    public string text;
}