using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionDisplay : MonoBehaviour {
    public Sprite noAction;
    public Sprite featureBackground;
    public Sprite sword;
    public Sprite arrow;
    public Sprite praxel;
    public Sprite pile;
    public Sprite sod;
    public Sprite taming;
    public Sprite roam;
    public Sprite station;

    public WorldInteraction interaction;
    private InputManager input;

    public Image currentAction;
    public Image currentCreature;
    public Transform hotbar;

    void Awake() {
        interaction.interactionChanged += InteractionChanged;
        interaction.creatureChanged += CreatureChanged;
    }

    void Start() {
        input = interaction.GetComponentStrict<InputManager>();
        for (int i = 0; i < 10; i++) {
            Transform creature = hotbar.GetChild(i).Find("Creature");
            creature.Find("Breastplate").GetComponent<Image>().color =
                GameManager.I.YourTeam.Color;
        }
        currentCreature.transform.Find("Breastplate").GetComponent<Image>().color =
            GameManager.I.YourTeam.Color;
        UpdateHotbar();
    }

    public void SelectAction(int id) => input.SelectAction(id);

    public void UpdateHotbar() {
        List<Interaction> actions = interaction.Actions();
        for (int i = 0; i < 10; i++) {
            Transform element = hotbar.GetChild(i);
            Transform creature = element.Find("Creature");
            if (i < actions.Count) {
                UpdateSprite(element.GetComponentStrict<Image>(), actions[i], false);
                creature.gameObject.SetActive(actions[i].IsCreatureAction);
                if (actions[i].IsCreatureAction) UpdateCreatureIcon(creature, actions[i].creature);
            } else {
                ClearSprite(element.GetComponentStrict<Image>());
                creature.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateCreatureIcon(Transform rootImage, Creature creature) {
        Image icon = rootImage.Find("Icon").GetComponent<Image>();
        icon.sprite = creature.icon;
        icon.SetNativeSize();
        Image breastplate = rootImage.Find("Breastplate").GetComponent<Image>();
        breastplate.sprite = creature.breastplate;
        breastplate.SetNativeSize();
    }

    private void InteractionChanged(Interaction action, Creature creature) {
        UpdateSprite(currentAction, action, true);

        List<Interaction> actions = interaction.Actions();
        for (int i = 0; i < 10; i++) {
            GameObject selected = hotbar.GetChild(i).Find("Selected").gameObject;
            selected.SetActive(i < actions.Count && action == actions[i]);
        }
    }

    private void CreatureChanged(Interaction action, Creature creature) {
        Debug.Log("Updating UI with creature " + creature + " so active should be " + (creature != null));
        currentCreature.gameObject.SetActive(creature != null);
        if (creature != null) UpdateCreatureIcon(currentCreature.transform, creature);

        UpdateHotbar();
        InteractionChanged(action, creature);
    }

    private void UpdateSprite(Image actionDisplay, Interaction action, bool selectedArea) {
        actionDisplay.sprite = GetSprite(action);
        Transform featureDisplay = actionDisplay.transform.Find("Feature");
        foreach (Transform feature in featureDisplay) GameObject.Destroy(feature.gameObject);
        if (action.IsCreatureAction && action.CreatureAction.UsesFeature) {
            Feature feature = GameObject.Instantiate(action.CreatureAction.feature, featureDisplay);
            feature.transform.SetLayer(LayerMask.NameToLayer("UI"));
            Cardboard cardboard = feature.transform.GetComponentInChildren<Cardboard>();
            if (cardboard != null) cardboard.EmbedInUI();
            else {
                feature.transform.localPosition = new Vector3(0, .25f, -.5f);
                feature.transform.localRotation = Quaternion.Euler(60, 0, -45);
                feature.transform.localScale = Vector3.one * Mathf.Sqrt(.5f);
                if (selectedArea) StartCoroutine(AnimateFeature(feature.transform));
            }
        }
    }

    private void ClearSprite(Image actionDisplay) {
        actionDisplay.sprite = noAction;
        Transform featureDisplay = actionDisplay.transform.Find("Feature");
        foreach (Transform feature in featureDisplay) GameObject.Destroy(feature.gameObject);
    }

    private Sprite GetSprite(Interaction action) {
        switch (action.mode) {
            case WorldInteraction.Mode.Sword:
                return sword;
            case WorldInteraction.Mode.Arrow:
                return arrow;
            case WorldInteraction.Mode.Praxel:
                return praxel;
            case WorldInteraction.Mode.WoodBuilding:
                return pile;
            case WorldInteraction.Mode.Sod:
                return sod;
            case WorldInteraction.Mode.Taming:
                return taming;
            case WorldInteraction.Mode.Directing:
                if (action.CreatureAction.isRoam) return roam;
                else if (action.CreatureAction.isStation) return station;
                else if (action.CreatureAction.UsesFeature) return featureBackground;
                else return action.CreatureAction.icon;
            default:
                throw new InvalidOperationException("Bad WorldInteraction Mode");
        }
    }

    private IEnumerator<YieldInstruction> AnimateFeature(Transform transform) {
        float z = -45;
        while (transform != null) {
            z += Time.deltaTime * 90;
            transform.localRotation = Quaternion.Euler(60, 0, z);
            yield return null;
        }
    }
}
