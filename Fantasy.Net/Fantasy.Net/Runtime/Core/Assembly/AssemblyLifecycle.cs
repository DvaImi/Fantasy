using System.Collections.Concurrent;
using System.Collections.Generic;
using Fantasy.Async;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 程序集生命周期管理类
    /// 管理所有注册的程序集生命周期回调，在程序集加载、卸载时触发相应的回调方法
    /// </summary>
    public static class AssemblyLifecycle
    {
#if FANTASY_WEBGL
        /// <summary>
        /// 程序集生命周期回调集合（WebGL 单线程版本）
        /// </summary>
        private static readonly Dictionary<IAssemblyLifecycle, byte> AssemblyLifecycles = new Dictionary<IAssemblyLifecycle, byte>();
#else
        /// <summary>
        /// 程序集生命周期回调集合（线程安全版本）
        /// 使用 ConcurrentDictionary 当作 Set 使用，Value 无实际意义
        /// </summary>
        private static readonly ConcurrentDictionary<IAssemblyLifecycle, byte> AssemblyLifecycles = new ConcurrentDictionary<IAssemblyLifecycle, byte>();
#endif
        /// <summary>
        /// 触发程序集加载事件
        /// 遍历所有已注册的生命周期回调，调用其 OnLoad 方法
        /// </summary>
        /// <param name="assemblyManifest">程序集清单对象</param>
        /// <returns>异步任务</returns>
        internal static async FTask OnLoad(AssemblyManifest assemblyManifest)
        {
            foreach (var (assemblyLifecycle, _) in AssemblyLifecycles)
            {
                await assemblyLifecycle.OnLoad(assemblyManifest);
            }
        }

        /// <summary>
        /// 触发程序集卸载事件
        /// 遍历所有已注册的生命周期回调，调用其 OnUnload 方法，并清理程序集清单
        /// </summary>
        /// <param name="assemblyManifest">程序集清单对象</param>
        /// <returns>异步任务</returns>
        internal static async FTask OnUnLoad(AssemblyManifest assemblyManifest)
        {
            foreach (var (assemblyLifecycle, _) in AssemblyLifecycles)
            {
                await assemblyLifecycle.OnUnload(assemblyManifest);
            }
            assemblyManifest.Clear();
        }

        /// <summary>
        /// 添加程序集生命周期回调
        /// 添加后会立即对所有已加载的程序集触发 Load 回调
        /// </summary>
        /// <param name="assemblyLifecycle">实现 IAssemblyLifecycle 接口的生命周期回调对象</param>
        internal static async FTask Add(IAssemblyLifecycle assemblyLifecycle)
        {
#if FANTASY_WEBGL
            AssemblyLifecycles.Add(assemblyLifecycle, 0);
#else
            AssemblyLifecycles.TryAdd(assemblyLifecycle, 0);
#endif
            foreach (var (_, assemblyManifest) in AssemblyManifest.Manifests)
            {
                await assemblyLifecycle.OnLoad(assemblyManifest);
            }
        }

        /// <summary>
        /// 移除程序集生命周期回调
        /// 移除后该回调将不再接收程序集的加载、卸载、重载事件
        /// </summary>
        /// <param name="assemblyLifecycle">要移除的生命周期回调对象</param>
        internal static void Remove(IAssemblyLifecycle assemblyLifecycle)
        {
            AssemblyLifecycles.Remove(assemblyLifecycle, out _);
        }

        /// <summary>
        /// 释放所有程序集生命周期回调
        /// 清空所有已注册的生命周期回调集合
        /// </summary>
        public static void Dispose()
        {
            AssemblyLifecycles.Clear();
        }
    }
}