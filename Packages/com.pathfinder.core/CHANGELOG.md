# Changelog

## [1.0.0] - 2026-03-19

### Added
- DIContainer with Singleton/Transient lifetime support
- DIContainerManager for global/scene container management
- Installer base class for MonoBehaviour-based service registration
- InjectAttribute and InjectOptionalAttribute for dependency injection
- RootContext for scene-level DI orchestration
- ServiceLifetime enum

### Interfaces
- IInteractable - interaction system interface
- IAbilityManager - player ability management
- IDeathManager - death/respawn handling
- ICheckpoint - checkpoint activation

### Enums
- AbilityType - player ability types (None, DoubleJump, Dash)