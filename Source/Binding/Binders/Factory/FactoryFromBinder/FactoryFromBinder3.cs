using System;
using System.Collections.Generic;
using UnityEngine;
using ModestTree;

namespace Zenject
{
    public class FactoryFromBinder<TParam1, TParam2, TParam3, TContract> : FactoryFromBinderBase
    {
        public FactoryFromBinder(
            DiContainer container, BindInfo bindInfo, FactoryBindInfo factoryBindInfo)
            : base(container, typeof(TContract), bindInfo, factoryBindInfo)
        {
        }

        public ConditionCopyNonLazyBinder FromMethod(Func<DiContainer, TParam1, TParam2, TParam3, TContract> method)
        {
            ProviderFunc =
                (container) => new MethodProviderWithContainer<TParam1, TParam2, TParam3, TContract>(method);

            return this;
        }

        // Shortcut for FromIFactory and also for backwards compatibility
        public ConditionCopyNonLazyBinder FromFactory<TSubFactory>()
            where TSubFactory : IFactory<TParam1, TParam2, TParam3, TContract>
        {
            return FromIFactory(x => x.To<TSubFactory>().AsCached());
        }

        public ArgConditionCopyNonLazyBinder FromIFactory(
            Action<ConcreteBinderGeneric<IFactory<TParam1, TParam2, TParam3, TContract>>> factoryBindGenerator)
        {
            Guid factoryId;
            factoryBindGenerator(
                CreateIFactoryBinder<IFactory<TParam1, TParam2, TParam3, TContract>>(out factoryId));

            ProviderFunc =
                (container) => { return new IFactoryProvider<TParam1, TParam2, TParam3, TContract>(container, factoryId); };

            return new ArgConditionCopyNonLazyBinder(BindInfo);
        }

        public FactorySubContainerBinder<TParam1, TParam2, TParam3, TContract> FromSubContainerResolve()
        {
            return FromSubContainerResolve(null);
        }

        public FactorySubContainerBinder<TParam1, TParam2, TParam3, TContract> FromSubContainerResolve(object subIdentifier)
        {
            return new FactorySubContainerBinder<TParam1, TParam2, TParam3, TContract>(
                BindContainer, BindInfo, FactoryBindInfo, subIdentifier);
        }

        public ArgConditionCopyNonLazyBinder FromMonoPoolableMemoryPool<TContractAgain>(
            Action<MemoryPoolInitialSizeMaxSizeBinder<TContractAgain>> poolBindGenerator)
            // Unfortunately we have to pass the same contract in again to satisfy the generic
            // constraints below
            where TContractAgain : Component, IPoolable<TParam1, TParam2, TParam3, IMemoryPool>
        {
            return FromPoolableMemoryPoolInternal<TContractAgain, MonoPoolableMemoryPool<TParam1, TParam2, TParam3, IMemoryPool, TContractAgain>>(poolBindGenerator);
        }

        public ArgConditionCopyNonLazyBinder FromPoolableMemoryPool<TContractAgain>(
            Action<MemoryPoolInitialSizeMaxSizeBinder<TContractAgain>> poolBindGenerator)
            // Unfortunately we have to pass the same contract in again to satisfy the generic
            // constraints below
            where TContractAgain : IPoolable<TParam1, TParam2, TParam3, IMemoryPool>
        {
            return FromPoolableMemoryPoolInternal<TContractAgain, PoolableMemoryPool<TParam1, TParam2, TParam3, IMemoryPool, TContractAgain>>(poolBindGenerator);
        }

        ArgConditionCopyNonLazyBinder FromPoolableMemoryPoolInternal<TContractAgain, TMemoryPool>(
            Action<MemoryPoolInitialSizeMaxSizeBinder<TContractAgain>> poolBindGenerator)
            // Unfortunately we have to pass the same contract in again to satisfy the generic
            // constraints below
            where TContractAgain : IPoolable<TParam1, TParam2, TParam3, IMemoryPool>
            where TMemoryPool : MemoryPool<TParam1, TParam2, TParam3, IMemoryPool, TContractAgain>
        {
            Assert.IsEqual(typeof(TContractAgain), typeof(TContract));

            // Use a random ID so that our provider is the only one that can find it and so it doesn't
            // conflict with anything else
            var poolId = Guid.NewGuid();

            var binder = BindContainer.BindMemoryPoolCustomInterface<TContractAgain, TMemoryPool, TMemoryPool>(
                false,
                // Very important here that we call StartBinding with false otherwise the other
                // binding will be finalized early
                BindContainer.StartBinding(null, false))
                .WithId(poolId);

            poolBindGenerator(binder);

            ProviderFunc =
                (container) => { return new PoolableMemoryPoolProvider<TParam1, TParam2, TParam3, TContractAgain, TMemoryPool>(container, poolId); };

            return new ArgConditionCopyNonLazyBinder(BindInfo);
        }
    }
}
