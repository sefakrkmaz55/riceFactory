// =============================================================================
// ServiceLocator.cs
// Basit Service Locator pattern ile dependency injection.
// Manager siniflarinin birbirini bulmasini saglar.
// Interface tabanli calisarak test edilebilirlik sunar.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiceFactory.Core
{
    /// <summary>
    /// Hafif Service Locator. Tam DI framework (Zenject/VContainer) yerine
    /// mobilde performans ve sadelik icin tercih edilmistir.
    /// Tum servisler interface uzerinden kayit edilir ve alinir.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        /// <summary>
        /// Bir servisi kayit eder. Ayni tip tekrar kaydedilirse uzerine yazilir.
        /// </summary>
        /// <typeparam name="T">Servis interface veya sinif tipi.</typeparam>
        /// <param name="service">Kaydedilecek servis instance'i.</param>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Null servis kaydi reddedildi: {typeof(T).Name}");
                return;
            }

            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] {type.Name} zaten kayitli, uzerine yaziliyor.");
            }

            _services[type] = service;
        }

        /// <summary>
        /// Kayitli bir servisi dondurur. Bulunamazsa exception firlatir.
        /// </summary>
        /// <typeparam name="T">Alinacak servis tipi.</typeparam>
        /// <returns>Kayitli servis instance'i.</returns>
        /// <exception cref="InvalidOperationException">Servis kayitli degilse.</exception>
        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            throw new InvalidOperationException(
                $"[ServiceLocator] {type.Name} kayitli degil. Boot sirasini kontrol edin."
            );
        }

        /// <summary>
        /// Guvenli servis erisimi. Servis bulunamazsa false dondurur.
        /// </summary>
        /// <typeparam name="T">Alinacak servis tipi.</typeparam>
        /// <param name="service">Bulunan servis veya null.</param>
        /// <returns>Servis bulunduysa true.</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var obj))
            {
                service = (T)obj;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Belirli bir servisin kayitli olup olmadigini kontrol eder.
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Belirli bir servisi kayittan kaldirir.
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);
            if (_services.Remove(type))
            {
                Debug.Log($"[ServiceLocator] {type.Name} kayittan kaldirildi.");
            }
        }

        /// <summary>
        /// Tum servisleri temizler. Test ortami ve sahne gecisleri icin.
        /// </summary>
        public static void Reset()
        {
            _services.Clear();
            Debug.Log("[ServiceLocator] Tum servisler temizlendi.");
        }
    }
}
