using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISectionSelectorComponentView
{
    /// <summary>
    /// Set the sections of the selector.设置选择器的各个部分
    /// </summary>
    /// <param name="sections">List of UI components.</param>
    void SetSections(List<SectionToggleModel> sections);

    /// <summary>
    /// Get a section of the section selector.获取部分选择器的一部分。
    /// </summary>
    /// <param name="index">Index of the list of sections.</param>
    /// <returns>A specific section toggle.</returns>
    ISectionToggle GetSection(int index);

    /// <summary>
    /// Get all the sections of the section selector.获取部分选择器的所有部分
    /// </summary>
    /// <returns>The list of sections.</returns>
    List<ISectionToggle> GetAllSections();
}

public class SectionSelectorComponentView : BaseComponentView, ISectionSelectorComponentView, IComponentModelConfig
{
    [Header("Prefab References")]
    [SerializeField] internal SectionToggle sectionToggleTemplate;//部分选择器切换模板

    [Header("Configuration")]
    [SerializeField] internal SectionSelectorComponentModel model;

    internal List<ISectionToggle> instantiatedSections = new List<ISectionToggle>();

    public override void Awake()
    {
        base.Awake();

        RegisterCurrentInstantiatedSections();//注册当前实例化的部分
    }

    public void Configure(BaseComponentModel newModel)
    {
        model = (SectionSelectorComponentModel)newModel;
        RefreshControl();
    }

    public override void RefreshControl()
    {
        if (model == null)
            return;

        SetSections(model.sections);
    }

    public void SetSections(List<SectionToggleModel> sections)
    {
        model.sections = sections;

        RemoveAllInstantiatedSections();

        for (int i = 0; i < sections.Count; i++)
        {
            CreateSection(sections[i], $"Section_{i}");
        }

        if (instantiatedSections.Count > 0)
            instantiatedSections[1].SelectToggle();
        
    }

    public ISectionToggle GetSection(int index)
    {
        if (index >= instantiatedSections.Count)
            return null;

        return instantiatedSections[index];
    }

    public List<ISectionToggle> GetAllSections() { return instantiatedSections; }

    internal void CreateSection(SectionToggleModel newSectionModel, string name)
    {
        if (sectionToggleTemplate == null)
            return;

        SectionToggle newGO = Instantiate(sectionToggleTemplate, transform);
        newGO.name = name;
        Debug.Log("1111111111111111111"+newGO.name);
        newGO.gameObject.SetActive(true);
        newGO.SetInfo(newSectionModel);
        newGO.SelectToggle();
        instantiatedSections.Add(newGO);
    }

    internal void RemoveAllInstantiatedSections()
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject == sectionToggleTemplate.gameObject)
                continue;

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                if (isActiveAndEnabled)
                    StartCoroutine(DestroyGameObjectOnEditor(child.gameObject));
            }
        }

        instantiatedSections.Clear();
    }

    internal IEnumerator DestroyGameObjectOnEditor(GameObject go)
    {
        yield return null;
        DestroyImmediate(go);
    }

    internal void RegisterCurrentInstantiatedSections()
    {
        instantiatedSections.Clear();

        foreach (Transform child in transform)
        {
            if (child.gameObject == sectionToggleTemplate.gameObject ||
                child.name.Contains("TooltipRef"))
                continue;

            SectionToggle existingSection = child.GetComponent<SectionToggle>();
            if (existingSection != null)
                instantiatedSections.Add(existingSection);
            else
                Destroy(child.gameObject);
        }
    }
}