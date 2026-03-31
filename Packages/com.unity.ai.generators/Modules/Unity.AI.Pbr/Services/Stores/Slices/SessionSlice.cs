using System;
using System.Linq;
using Unity.AI.Pbr.Services.SessionPersistence;
using Unity.AI.Pbr.Services.Stores.Actions;
using Unity.AI.Pbr.Services.Stores.States;
using Unity.AI.Generators.Redux;
using Unity.AI.Toolkit.Utility;
using Unity.AI.Pbr.Services.Utilities;

namespace Unity.AI.Pbr.Services.Stores.Slices
{
    static class SessionSlice
    {
        public static void Create(Store store)
        {
            var persistedSession = MaterialGeneratorSettings.instance.session;
            var initialState = persistedSession != null ? persistedSession with { } : new Session();

            store.CreateSlice(
                SessionActions.slice,
                initialState,
                reducers => reducers
                    .AddCase(SessionActions.setPreviewSizeFactor, (state, payload) =>
                    {
                        if (state?.settings?.previewSettings != null)
                            state.settings.previewSettings.sizeFactor = payload.payload;
                    }),
                extraReducers => extraReducers
                    .AddCase(AppActions.init).With((_, payload) =>
                    {
                        var mergedState = payload.payload.sessionSlice != null
                            ? payload.payload.sessionSlice with { }
                            : new Session();

                        var persisted = MaterialGeneratorSettings.instance.session;

                        if (mergedState == null)
                            return new Session();

                        if (mergedState.settings == null)
                            return mergedState;

                        if (persisted?.settings?.lastSelectedModels != null)
                        {
                            if (mergedState.settings.lastSelectedModels == null)
                                return mergedState;

                            foreach (var kvp in persisted.settings.lastSelectedModels)
                            {
                                var modelSelection = mergedState.settings.lastSelectedModels.Ensure(kvp.Key);
                                if (modelSelection != null && string.IsNullOrEmpty(modelSelection.modelID))
                                    modelSelection.modelID = kvp.Value?.modelID ?? string.Empty;
                            }
                        }

                        if (persisted?.settings?.lastMaterialMappings != null)
                            mergedState.settings.lastMaterialMappings = persisted.settings.lastMaterialMappings;

                        if (persisted?.settings?.previewSettings != null &&
                            mergedState.settings.previewSettings != null)
                        {
                            mergedState.settings.previewSettings.sizeFactor =
                                persisted.settings.previewSettings.sizeFactor;
                        }

                        return mergedState;
                    })
                    .AddCase(GenerationSettingsActions.setSelectedModelID).With((state, payload) =>
                    {
                        if (state?.settings?.lastSelectedModels != null)
                            state.settings.lastSelectedModels.Ensure(payload.payload.mode).modelID = payload.payload.modelID;
                    })
                    .Add(GenerationResultsActions.setGeneratedMaterialMapping, (state, payload) =>
                    {
                        var material = payload.asset.GetMaterialAdapter();
                        if (material == null || state?.settings?.lastMaterialMappings == null)
                            return;

                        var shaderAssetCache = state.settings.lastMaterialMappings.Ensure(material.Shader);
                        if (shaderAssetCache == null)
                            return;

                        var mapType = payload.mapType;
                        var materialProperty = payload.materialProperty;
                        shaderAssetCache[mapType] = materialProperty;
                    }),
                state =>
                {
                    if (state?.settings == null)
                        return state;

                    return state with
                    {
                        settings = state.settings with
                        {
                            lastSelectedModels = new SerializableDictionary<RefinementMode, ModelSelection>(
                                (state.settings.lastSelectedModels ?? new SerializableDictionary<RefinementMode, ModelSelection>())
                                .ToDictionary(
                                    kvp => kvp.Key,
                                    kvp => (kvp.Value ?? new ModelSelection()) with
                                    {
                                        modelID = kvp.Value?.modelID ?? string.Empty
                                    })),

                            lastMaterialMappings = new SerializableDictionary<string, SerializableDictionary<MapType, string>>(
                                (state.settings.lastMaterialMappings ?? new SerializableDictionary<string, SerializableDictionary<MapType, string>>())
                                .ToDictionary(
                                    kvp => kvp.Key,
                                    kvp => new SerializableDictionary<MapType, string>(
                                        (kvp.Value ?? new SerializableDictionary<MapType, string>())
                                        .ToDictionary(
                                            keyValuePair => keyValuePair.Key,
                                            keyValuePair => keyValuePair.Value)))),

                            previewSettings = state.settings.previewSettings == null
                                ? null
                                : state.settings.previewSettings with
                                {
                                    sizeFactor = state.settings.previewSettings.sizeFactor
                                }
                        }
                    };
                });
        }
    }
}