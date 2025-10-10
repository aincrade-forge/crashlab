using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Profiling;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UI;

namespace CrashLab.Actions
{
    public static class AssetBundleFloodRunner
    {
        public const string FloodLabel = "crashlab.asset_flood";
        private const string BreadcrumbCategory = "crashlab.asset_bundle_flood";

        private static bool _active;
        private static readonly List<AsyncOperationHandle<UnityEngine.Object>> LoadedHandles = new();
        private static readonly List<AsyncOperationHandle<GameObject>> InstantiatedHandles = new();
        private static readonly List<UnityEngine.Object> DuplicatedAssets = new();
        private static readonly List<byte[]> MemoryBlocks = new();

        public static void Run(bool forceOutOfMemory = true)
        {
            if (_active)
            {
                Debug.LogWarning("CRASHLAB::asset_bundle_flood::RUNNING");
                CrashLabBreadcrumbs.Warning("Asset bundle flood already running", BreadcrumbCategory);
                return;
            }

            _active = true;
            CrashLabBreadcrumbs.Info("Asset bundle flood requested", BreadcrumbCategory,
                new Dictionary<string, string> { { "force_oom", forceOutOfMemory.ToString() } });

            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle = default;
            try
            {
                locationsHandle = Addressables.LoadResourceLocationsAsync(FloodLabel);
                var locations = locationsHandle.WaitForCompletion();
                if (locations == null || locations.Count == 0)
                {
                    Debug.LogWarning($"CRASHLAB::asset_bundle_flood::NO_LOCATIONS::{FloodLabel}. Run 'CrashLab/Addressables/Sync Flood Assets' to register assets.");
                    CrashLabBreadcrumbs.Warning("No addressable assets tagged for flood", BreadcrumbCategory,
                        new Dictionary<string, string> { { "label", FloodLabel } });
                    return;
                }

                var successLoads = 0;
                var pass = 0;
                var maxPasses = forceOutOfMemory ? int.MaxValue : 1;

                while (pass < maxPasses)
                {
                    pass++;
                    foreach (var location in locations)
                    {
                        var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(location);
                        var asset = handle.WaitForCompletion();
                        if (asset == null)
                        {
                            Debug.LogWarning($"CRASHLAB::asset_bundle_flood::LOAD_FAIL::{location.PrimaryKey}");
                            CrashLabBreadcrumbs.Warning("Addressables load returned null", BreadcrumbCategory,
                                new Dictionary<string, string> { { "location", location.PrimaryKey } });
                            Addressables.Release(handle);
                            continue;
                        }

                        LoadedHandles.Add(handle);
                        successLoads++;

                        if (forceOutOfMemory)
                        {
                            TryDuplicateAsset(location, asset);
                        }
                    }

                    var allocatedMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                    Debug.Log($"CRASHLAB::asset_bundle_flood::PASS::{pass}::loaded={successLoads}::mem={allocatedMb:F1}MB");
                    CrashLabBreadcrumbs.Info("Asset bundle flood pass complete", BreadcrumbCategory,
                        new Dictionary<string, string>
                        {
                            { "pass", pass.ToString() },
                            { "success_loads", successLoads.ToString() },
                            { "allocated_mb", allocatedMb.ToString("F1") }
                        });
                }

                Debug.Log($"CRASHLAB::asset_bundle_flood::COMPLETE::loads={successLoads}::passes={pass}");
                CrashLabBreadcrumbs.Info("Asset bundle flood finished", BreadcrumbCategory,
                    new Dictionary<string, string>
                    {
                        { "loads", successLoads.ToString() },
                        { "passes", pass.ToString() }
                    });
            }
            catch (OutOfMemoryException oom)
            {
                Debug.LogError("CRASHLAB::asset_bundle_flood::OOM");
                CrashLabBreadcrumbs.Error("Asset bundle flood triggered OutOfMemoryException", BreadcrumbCategory,
                    new Dictionary<string, string> { { "exception_message", oom.Message } });
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CRASHLAB::asset_bundle_flood::ERROR::{ex.GetType().Name}:{ex.Message}");
                Debug.LogException(ex);
                CrashLabBreadcrumbs.Error("Asset bundle flood failed", BreadcrumbCategory,
                    new Dictionary<string, string>
                    {
                        { "exception_type", ex.GetType().Name },
                        { "exception_message", ex.Message }
                    });
            }
            finally
            {
                Cleanup();

                if (locationsHandle.IsValid())
                {
                    Addressables.Release(locationsHandle);
                }

                _active = false;
                Debug.Log("CRASHLAB::asset_bundle_flood::END");
            }
        }

        private static void TryDuplicateAsset(IResourceLocation location, UnityEngine.Object asset)
        {
            try
            {
                if (asset is GameObject)
                {
                    var instanceHandle = Addressables.InstantiateAsync(location);
                    var instance = instanceHandle.WaitForCompletion();
                    if (instance != null)
                    {
                        InstantiatedHandles.Add(instanceHandle);
                    }
                    else
                    {
                        Addressables.Release(instanceHandle);
                    }
                }
                else if (asset is Sprite sprite)
                {
                    var go = new GameObject($"FloodSprite::{location.PrimaryKey}");
                    var image = go.AddComponent<Image>();
                    image.sprite = sprite;
                    DuplicatedAssets.Add(go);
                }
                else if (asset is Texture texture)
                {
                    var go = new GameObject($"FloodTexture::{location.PrimaryKey}");
                    var rawImage = go.AddComponent<RawImage>();
                    rawImage.texture = texture;
                    DuplicatedAssets.Add(go);
                }
                else
                {
                    var size = Profiler.GetRuntimeMemorySizeLong(asset);
                    if (size <= 0)
                    {
                        size = 256;
                    }

                    try
                    {
                        MemoryBlocks.Add(new byte[size]);
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CRASHLAB::asset_bundle_flood::DUPLICATE_FAIL::{location.PrimaryKey}::{ex.GetType().Name}:{ex.Message}");
                CrashLabBreadcrumbs.Warning("Failed to duplicate asset during flood", BreadcrumbCategory,
                    new Dictionary<string, string>
                    {
                        { "location", location.PrimaryKey },
                        { "exception_type", ex.GetType().Name }
                    });
            }
        }

        private static void Cleanup()
        {
            try
            {
                for (int i = 0; i < LoadedHandles.Count; i++)
                {
                    Addressables.Release(LoadedHandles[i]);
                }

                for (int i = 0; i < InstantiatedHandles.Count; i++)
                {
                    Addressables.Release(InstantiatedHandles[i]);
                }
            }
            catch (Exception releaseEx)
            {
                Debug.LogWarning($"CRASHLAB::asset_bundle_flood::RELEASE_FAIL::{releaseEx.GetType().Name}:{releaseEx.Message}");
                CrashLabBreadcrumbs.Warning("Failed to release addressable handles", BreadcrumbCategory,
                    new Dictionary<string, string>
                    {
                        { "exception_type", releaseEx.GetType().Name }
                    });
            }
            finally
            {
                LoadedHandles.Clear();
                InstantiatedHandles.Clear();

                for (int i = 0; i < DuplicatedAssets.Count; i++)
                {
                    var clone = DuplicatedAssets[i];
                    if (clone == null)
                    {
                        continue;
                    }

                    try
                    {
                        if (clone is GameObject go)
                        {
                            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                            else UnityEngine.Object.DestroyImmediate(go);
                        }
                        else
                        {
                            if (Application.isPlaying) UnityEngine.Object.Destroy(clone);
                            else UnityEngine.Object.DestroyImmediate(clone);
                        }
                    }
                    catch
                    {
                        // best-effort cleanup
                    }
                }

                DuplicatedAssets.Clear();
                MemoryBlocks.Clear();
            }
        }
    }
}
