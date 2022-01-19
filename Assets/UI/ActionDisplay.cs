using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionDisplay : MonoBehaviour {
    public Sprite noAction;
    public Sprite sword;
    public Sprite arrow;
    public Sprite praxel;
    public Sprite pile;
    public Sprite sod;
    public Sprite taming;
    public Sprite roam;
    public Sprite station;

    public WorldInteraction interaction;

    public Image currentAction;
    public Image currentCreature;
    public Transform hotbar;

    void Awake() {
        interaction.interactionChanged += InteractionChanged;
    }

    void Start() {
        for (int i = 0; i < 10; i++) {
            Transform creature = hotbar.GetChild(i).Find("Creature");
            creature.Find("Breastplate").GetComponent<Image>().color =
                GameObject.FindObjectOfType<PlayerCharacter>().GetComponent<Team>().Color;
        }
        currentCreature.transform.Find("Breastplate").GetComponent<Image>().color =
            GameObject.FindObjectOfType<PlayerCharacter>().GetComponent<Team>().Color;
        UpdateHotbar();
    }

    public void UpdateHotbar() {
        List<Interaction> actions = interaction.Actions();
        for (int i = 0; i < 10; i++) {
            Transform element = hotbar.GetChild(i);
            Transform creature = element.Find("Creature");
            if (i < actions.Count) {
                element.GetComponent<Image>().sprite = GetSprite(actions[i]);
                creature.gameObject.SetActive(actions[i].IsCreatureAction);
                if (actions[i].IsCreatureAction) UpdateCreatureIcon(creature, actions[i].creature);
            } else {
                element.GetComponent<Image>().sprite = noAction;
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
        Debug.Log("Updating UI with creature " + creature + " so active should be " + (creature != null));
        currentAction.sprite = GetSprite(action);
        currentCreature.gameObject.SetActive(creature != null);
        if (creature != null) UpdateCreatureIcon(currentCreature.transform, creature);

        List<Interaction> actions = interaction.Actions();
        for (int i = 0; i < 10; i++) {
            GameObject selected = hotbar.GetChild(i).Find("Selected").gameObject;
            selected.SetActive(i < actions.Count && action == actions[i]);
        }
        UpdateHotbar();
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
                else return action.CreatureAction.icon;
            default:
                throw new InvalidOperationException("Bad WorldInteraction Mode");
        }
    }
}
