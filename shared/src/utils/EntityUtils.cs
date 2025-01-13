using Godot;
using System;

namespace SteampunkDnD.Shared;

public static class EntityUtils
{
    private static readonly string CharacterFolderPath = "res://shared/scenes/entities/characters/kinds";
    private static readonly string StaticPropFolderPath = "res://shared/scenes/entities/static_props/kinds";

    public static IEntity CreateEntity(this EntityState state)
    {
        switch (state)
        {
            case StaticState staticState:
                string fileName = staticState.Kind.CamelToSnakeCase();
                string path = $"{StaticPropFolderPath}/{fileName}.tscn";
                if (!ResourceLoader.Exists(path))
                    throw new ArgumentException($"File for {staticState.Kind} static prop kind does not exist");

                var packedScene = GD.Load<PackedScene>(path);
                var staticProp = packedScene.Instantiate() as StaticProp;
                staticProp.ApplyState(staticState);
                return staticProp;
            case CharacterState characterState:
                fileName = characterState.Kind.CamelToSnakeCase();
                path = $"{CharacterFolderPath}/{fileName}.tscn";
                if (!ResourceLoader.Exists(path))
                    throw new ArgumentException($"File for {characterState.Kind} character kind does not exist");

                packedScene = GD.Load<PackedScene>(path);
                var character = packedScene.Instantiate() as Character;
                character.ApplyState(characterState);
                return character;
            default:
                throw new NotImplementedException($"Entity creation from {state.GetType().Name} wasn't implemented");
        }
    }

    public static EntityState Interpolate(this EntityState past, EntityState future, float theta)
    {
        if (past.EntityId != future.EntityId)
            throw new ArgumentException("Passed future state has different EntityId value");

        switch (past)
        {
            case StaticState pastStaticState:
                if (future is not StaticState futureStaticState)
                    throw new ArgumentException("Passed future state has different type");

                if (pastStaticState.Kind != futureStaticState.Kind)
                    Logger.Singleton.Log(LogLevel.Error, "Passed future state has different Kind value");

                return pastStaticState with
                {
                    Position = pastStaticState.Position.Lerp(futureStaticState.Position, theta),
                    Rotation = Quaternion.FromEuler(pastStaticState.Rotation)
                        .Slerp(Quaternion.FromEuler(futureStaticState.Rotation), theta).GetEuler()
                };

            case CharacterState pastCharacterState:
                if (future is not CharacterState futureCharacterState)
                    throw new ArgumentException("Passed future state has different type");

                if (pastCharacterState.Kind != futureCharacterState.Kind)
                    Logger.Singleton.Log(LogLevel.Error, "Passed future state has different Kind value");

                return pastCharacterState with
                {
                    Position = pastCharacterState.Position.Lerp(futureCharacterState.Position, theta),
                    Rotation = Quaternion.FromEuler(pastCharacterState.Rotation)
                        .Slerp(Quaternion.FromEuler(futureCharacterState.Rotation), theta).GetEuler(),
                    Velocity = pastCharacterState.Velocity.Lerp(futureCharacterState.Velocity, theta)
                };
            default:
                throw new NotImplementedException($"Interpolation for {past.GetType().Name} wasn't implemented");
        }
    }
}
