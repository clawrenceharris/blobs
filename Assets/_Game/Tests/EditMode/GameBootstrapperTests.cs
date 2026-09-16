using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blobs.Content;
using Blobs.Core;
using Blobs.Input;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Blobs.Tests.EditMode
{
    public sealed class GameBootstrapperTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    Object.DestroyImmediate(_createdObjects[i]);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void SuccessfulMergeLeavesSourceVisibleAtTargetPosition()
        {
            var shell = CreateSceneShell();

            shell.Input.SelectBlobAt(new GridPosition(0, 0));
            shell.Input.SelectBlobAt(new GridPosition(2, 0));

            Assert.That(shell.Board.VisibleBlobCount, Is.EqualTo(1));
            Assert.That(shell.Board.CurrentSnapshot.Board.Blobs.Single().Id, Is.EqualTo("0"));
            Assert.That(shell.Board.CurrentSnapshot.Board.Blobs.Single().Position, Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(shell.Board.CurrentSnapshot.IsComplete, Is.False);
            Assert.That(shell.Board.IsSynchronizedWith(shell.Board.CurrentSnapshot), Is.True);
        }


        [Test]
        public void RestartRestoresInitialVisibleBlobsAndClearsSelection()
        {
            var shell = CreateSceneShell();
            shell.Input.SelectBlobAt(new GridPosition(0, 0));
            shell.Input.SelectBlobAt(new GridPosition(2, 0));

            shell.Commands.Restart();
            shell.Input.SelectBlobAt(new GridPosition(0, 0));
            var secondSelection = shell.Input.SelectBlobAt(new GridPosition(0, 0));

            Assert.That(shell.Board.VisibleBlobCount, Is.EqualTo(2));
            Assert.That(shell.Board.CurrentSnapshot.MoveCount, Is.EqualTo(0));
            Assert.That(secondSelection.HasSelection, Is.False);
            Assert.That(secondSelection.MoveAttempted, Is.False);
            Assert.That(shell.Board.IsSynchronizedWith(shell.Board.CurrentSnapshot), Is.True);
        }

        [Test]
        public void MissingRequiredSceneCollaboratorsAbortWithoutRuntimeFallbacks()
        {
            GameObject root = CreateGameObject("Incomplete Scene Shell");
            GameBootstrapper bootstrapper = root.AddComponent<GameBootstrapper>();
            LevelDefinitionAsset level = CreateLevelAsset();

            LogAssert.Expect(
                LogType.Error,
                "GameBootstrapper requires authored BoardPresenter, " +
                "GameplayInputAdapter, and GameplayCommandAdapter references.");

            bootstrapper.StartLevel(level);

            Assert.That(bootstrapper.CurrentState, Is.Null);
            Assert.That(root.GetComponent<BoardPresenter>(), Is.Null);
            Assert.That(root.GetComponent<GameplayInputAdapter>(), Is.Null);
            Assert.That(root.GetComponent<GameplayCommandAdapter>(), Is.Null);
        }

        private SceneShell CreateSceneShell()
        {
            var root = CreateGameObject("Scene Shell");
            var boardObject = CreateGameObject("Board Presenter");
            boardObject.transform.SetParent(root.transform);
            var inputObject = CreateGameObject("Input Adapter");
            inputObject.transform.SetParent(root.transform);

            var board = TestPresentationComposition.AddBoardPresenter(boardObject);
            var input = inputObject.AddComponent<GameplayInputAdapter>();
            var bootstrapper = root.AddComponent<GameBootstrapper>();
            var commands = root.AddComponent<GameplayCommandAdapter>();
            var level = CreateLevelAsset();
            var viewCatalog = AssetDatabase.LoadAssetAtPath<ViewCatalogAsset>(
                "Assets/_Game/Production/Content/Presentation/ViewCatalog.asset");

            Assert.That(viewCatalog, Is.Not.Null);

            SetPrivateField(bootstrapper, "boardPresenter", board);
            SetPrivateField(bootstrapper, "inputAdapter", input);
            SetPrivateField(bootstrapper, "commandAdapter", commands);
            SetPrivateField(bootstrapper, "levelAsset", level);
            SetPrivateField(
                board.GetComponent<BlobPresenter>(),
                "_viewCatalog",
                viewCatalog);

            bootstrapper.StartLevel(level);

            Assert.That(board.VisibleBlobCount, Is.EqualTo(2));
            return new SceneShell(bootstrapper, commands, board, input);
        }

        private LevelDefinitionAsset CreateLevelAsset()
        {
            var asset = ScriptableObject.CreateInstance<LevelDefinitionAsset>();
            _createdObjects.Add(asset);

            SetPrivateField(asset, "levelId", "bootstrapper-test");
            SetPrivateField(asset, "schemaVersion", 1);
            SetPrivateField(asset, "width", 3);
            SetPrivateField(asset, "height", 1);
            SetPrivateField(asset, "palette", CreatePaletteAsset());
            SetPrivateField(asset, "blobs", new List<BlobAssetData>
            {
                new NormalBlobAssetData
                {
                    id = "0",
                    position = new Vector2Int(0, 0),
                    color = BlobColor.Red,
                    type = BlobType.Normal,
                },
                new NormalBlobAssetData
                {
                    id = "1",
                    position = new Vector2Int(2, 0),
                    color = BlobColor.Blue,
                    type = BlobType.Normal,
                }
            });
            SetPrivateField(asset, "tiles", new List<TileAssetData>());

            return asset;
        }

        private LevelColorPaletteAsset CreatePaletteAsset()
        {
            var theme = ScriptableObject.CreateInstance<LevelColorPaletteAsset>();
            _createdObjects.Add(theme);
            return theme;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + fieldName);
            field.SetValue(target, value);
        }

        private readonly struct SceneShell
        {
            public SceneShell(
                GameBootstrapper bootstrapper,
                GameplayCommandAdapter commands,
                BoardPresenter board,
                GameplayInputAdapter input)
            {
                Bootstrapper = bootstrapper;
                Commands = commands;
                Board = board;
                Input = input;
            }

            public GameBootstrapper Bootstrapper { get; }
            public GameplayCommandAdapter Commands { get; }
            public BoardPresenter Board { get; }
            public GameplayInputAdapter Input { get; }
        }
    }
}
