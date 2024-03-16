using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace VoiceVolume;

[HarmonyPatch]
public class VoiceVolumeMod : ModSystem {
    private Harmony? _harmony;

    private static int PlayerVoiceLevel {
        get => ClientSettings.Inst.GetIntSetting("playerVoiceLevel");
        set {
            ClientSettings.Inst.Int["playerVoiceLevel"] = value;
            ClientSettings.Inst.Save(true);
        }
    }

    private static int TraderVoiceLevel {
        get => ClientSettings.Inst.GetIntSetting("traderVoiceLevel");
        set {
            ClientSettings.Inst.Int["traderVoiceLevel"] = value;
            ClientSettings.Inst.Save(true);
        }
    }

    public override bool ShouldLoad(EnumAppSide side) {
        return side.IsClient();
    }

    public override void StartClientSide(ICoreClientAPI api) {
        if (!ClientSettings.Inst.Int.Exists("playerVoiceLevel")) {
            PlayerVoiceLevel = 100;
        }

        if (!ClientSettings.Inst.Int.Exists("traderVoiceLevel")) {
            TraderVoiceLevel = 100;
        }

        _harmony = new Harmony(Mod.Info.ModID);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public override void Dispose() {
        _harmony?.UnpatchAll(Mod.Info.ModID);
    }

    private static float _vanillaModifier;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EntityTalkUtil), "PlaySound", typeof(float), typeof(float), typeof(float), typeof(float), typeof(float))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static void PrePlaySound(EntityTalkUtil __instance) {
        _vanillaModifier = __instance.volumneModifier;

        int voiceLevel;
        switch (__instance.GetField<Entity>("entity")) {
            case EntityPlayer:
                voiceLevel = PlayerVoiceLevel;
                break;
            case EntityTrader:
                voiceLevel = TraderVoiceLevel;
                break;
            default:
                return;
        }

        __instance.volumneModifier = _vanillaModifier * (voiceLevel / 100F);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EntityTalkUtil), "PlaySound", typeof(float), typeof(float), typeof(float), typeof(float), typeof(float))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static void PostPlaySound(EntityTalkUtil __instance) {
        __instance.volumneModifier = _vanillaModifier;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiCompositeSettings), "OnSoundOptions")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static bool PreOnSoundOptions(GuiCompositeSettings __instance) {
        ElementBounds leftText = ElementBounds.Fixed(0.0, 87.0, 210.0, 40.0);
        ElementBounds rightSlider = ElementBounds.Fixed(220.0, 89.0, 350.0, 20.0);

        string[] devices = new string[1].Append(ScreenManager.Platform.AvailableAudioDevices.ToArray<string>());
        string[] devicesnames = new[] {
            "Default"
        }.Append(ScreenManager.Platform.AvailableAudioDevices.ToArray<string>());

        GuiComposer composer = __instance.Invoke<GuiComposer>("ComposerHeader", new object[] { "gamesettings-soundoptions", "sounds" })
            .AddStaticText(Lang.Get("setting-name-soundlevel"), CairoFont.WhiteSmallishText(), leftText = leftText.FlatCopy())
            .AddSlider(i => __instance.Invoke<bool>("onSoundLevelChanged", new object[] { i }), rightSlider = rightSlider.FlatCopy(), "soundLevel")
            .AddStaticText(Lang.Get("setting-name-ambientsoundlevel"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy())
            .AddSlider(i => __instance.Invoke<bool>("onAmbientSoundLevelChanged", new object[] { i }), rightSlider = rightSlider.BelowCopy(0.0, 21.0), "ambientSoundLevel")
            .AddStaticText(Lang.Get("setting-name-weathersoundlevel"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy())
            .AddSlider(i => __instance.Invoke<bool>("onWeatherSoundLevelChanged", new object[] { i }), rightSlider = rightSlider.BelowCopy(0.0, 21.0), "weatherSoundLevel")
            .AddStaticText(Lang.Get("voicevolume:setting-name-playersoundlevel"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 22.0))
            .AddSlider(i => {
                PlayerVoiceLevel = i;
                return true;
            }, rightSlider = rightSlider.BelowCopy(0.0, 41.0), "playerVoiceSoundLevel")
            .AddStaticText(Lang.Get("voicevolume:setting-name-tradersoundlevel"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy())
            .AddSlider(i => {
                TraderVoiceLevel = i;
                return true;
            }, rightSlider = rightSlider.BelowCopy(0.0, 21.0), "traderVoiceSoundLevel")
            .AddStaticText(Lang.Get("setting-name-musiclevel"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 22.0))
            .AddSlider(i => __instance.Invoke<bool>("onMusicLevelChanged", new object[] { i }), rightSlider = rightSlider.BelowCopy(0.0, 41.0), "musicLevel")
            .AddStaticText(Lang.Get("setting-name-musicfrequency"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy())
            .AddSlider(i => __instance.Invoke<bool>("onMusicFrequencyChanged", new object[] { i }), rightSlider = rightSlider.BelowCopy(0.0, 21.0), "musicFrequency")
            .AddStaticText(Lang.Get("setting-name-audiooutputdevice"), CairoFont.WhiteSmallishText(), leftText.BelowCopy(0.0, 22.0))
            .AddDropDown(devices, devicesnames, 0, (code, selected) => __instance.InvokeVoid("onAudioDeviceChanged", new object?[] { code, selected }), rightSlider.BelowCopy(0.0, 36.0).WithFixedSize(300.0, 30.0), "audiooutputdevice")
            .EndChildElements()
            .Compose();

        __instance.SetField("composer", composer);
        __instance.GetField<IGameSettingsHandler>("handler")!.LoadComposer(composer);

        composer.GetSlider("soundLevel").SetValues(ClientSettings.SoundLevel, 0, 100, 1, "%");
        composer.GetSlider("ambientSoundLevel").SetValues(ClientSettings.AmbientSoundLevel, 0, 100, 1, "%");
        composer.GetSlider("weatherSoundLevel").SetValues(ClientSettings.WeatherSoundLevel, 0, 100, 1, "%");
        composer.GetSlider("playerVoiceSoundLevel").SetValues(PlayerVoiceLevel, 0, 100, 1, "%");
        composer.GetSlider("traderVoiceSoundLevel").SetValues(TraderVoiceLevel, 0, 100, 1, "%");
        composer.GetSlider("musicLevel").SetValues(ClientSettings.MusicLevel, 0, 100, 1, "%");

        composer.GetSlider("musicFrequency").OnSliderTooltip = value => new[] {
            Lang.Get("setting-musicfrequency-low"),
            Lang.Get("setting-musicfrequency-medium"),
            Lang.Get("setting-musicfrequency-often"),
            Lang.Get("setting-musicfrequency-veryoften")
        }[value];
        composer.GetSlider("musicFrequency").SetValues(ClientSettings.MusicFrequency, 0, 3, 1);
        composer.GetDropDown("audiooutputdevice").SetSelectedIndex(Math.Max(0, devices.IndexOf(ClientSettings.AudioDevice)));

        return false;
    }
}
