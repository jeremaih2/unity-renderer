using DCL.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class AvatarModel : BaseModel
{
    public string id;
    public string name;
    public string bodyShape;
    public Color skinColor;
    public Color hairColor;
    public Color eyeColor;
    public List<string> wearables = new List<string>();
    public string expressionTriggerId = null;
    public long expressionTriggerTimestamp = -1;
    public string stickerTriggerId = null;
    public long stickerTriggerTimestamp = -1;
    public bool talking = false;
//其他的avatar模型是否有相同颜色
    public bool HaveSameWearablesAndColors(AvatarModel other)
    {
        if (other == null)
            return false;

        bool wearablesAreEqual = wearables.All(other.wearables.Contains) && wearables.Count == other.wearables.Count;

        return bodyShape == other.bodyShape &&
               skinColor == other.skinColor &&
               hairColor == other.hairColor &&
               eyeColor == other.eyeColor &&
               wearablesAreEqual;
    }
//其他avatar模型是否有相同的表示
    public bool HaveSameExpressions(AvatarModel other)
    {
        return expressionTriggerId == other.expressionTriggerId &&
               expressionTriggerTimestamp == other.expressionTriggerTimestamp &&
               stickerTriggerTimestamp == other.stickerTriggerTimestamp;
    }
//其他avatar模型是否相同
    public bool Equals(AvatarModel other)
    {
        bool wearablesAreEqual = wearables.All(other.wearables.Contains) && wearables.Count == other.wearables.Count;

        return id == other.id &&
               name == other.name &&
               bodyShape == other.bodyShape &&
               skinColor == other.skinColor &&
               hairColor == other.hairColor &&
               eyeColor == other.eyeColor &&
               expressionTriggerId == other.expressionTriggerId &&
               expressionTriggerTimestamp == other.expressionTriggerTimestamp &&
               stickerTriggerTimestamp == other.stickerTriggerTimestamp &&
               wearablesAreEqual;
    }
//更改模型时复制相同属性
    public void CopyFrom(AvatarModel other)
    {
        if (other == null)
            return;

        id = other.id;
        name = other.name;
        bodyShape = other.bodyShape;
        skinColor = other.skinColor;
        hairColor = other.hairColor;
        eyeColor = other.eyeColor;
        expressionTriggerId = other.expressionTriggerId;
        expressionTriggerTimestamp = other.expressionTriggerTimestamp;
        stickerTriggerId = other.stickerTriggerId;
        stickerTriggerTimestamp = other.stickerTriggerTimestamp;
        wearables = new List<string>(other.wearables);
    }
//基础模型从json表中获取数据
    public override BaseModel GetDataFromJSON(string json) { return Utils.SafeFromJson<AvatarModel>(json); }
}