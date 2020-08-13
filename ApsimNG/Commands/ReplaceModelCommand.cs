﻿namespace UserInterface.Commands
{
    using System;
    using Models.Core;
    using Interfaces;
    using Presenters;

    /// <summary>
    /// This command replaces a model with a different model object
    /// </summary>
    class ReplaceModelCommand : ICommand
    {
        /// <summary>The explorer view</summary>
        private IExplorerView explorerView;

        // <summary>The explorer presenter</summary>
        private ExplorerPresenter presenter;

        /// <summary>The model to be replaced</summary>
        private IModel modelToReplace;

        /// <summary>The new model we're to insert.</summary>
        private IModel modelToInsert;

        /// <summary>The model was moved</summary>
        private bool modelWasReplaced;

        /// <summary>Constructor.</summary>
        /// <param name="modelToReplace">The model to move.</param>
        /// <param name="modelToInsert">The new model to put in place of the old one</param>
        /// <param name="presenter">The explorer presenter.</param>
        public ReplaceModelCommand(IModel modelToReplace, IModel modelToInsert, ExplorerPresenter presenter)
        {
            if (modelToReplace.ReadOnly)
                throw new ApsimXException(modelToReplace, string.Format("Unable to replace {0} - it is read-only.", modelToReplace.Name));
            this.modelToReplace = modelToReplace;
            this.modelToInsert = modelToInsert;
            this.explorerView = presenter.GetView() as IExplorerView; 
            this.presenter = presenter;
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            IModel parent = modelToReplace.Parent as IModel;
            int modelIndex = parent.Children.IndexOf(modelToReplace as Model);

            // Replace model.
            try
            {
                this.explorerView.Tree.Delete(this.modelToReplace.FullPath);
                parent.Children.Remove(modelToReplace as Model);
                parent.Children.Insert(modelIndex, modelToInsert as Model);
                modelToInsert.Parent = parent;
                var nodeDescription = presenter.GetNodeDescription(modelToInsert);
                this.explorerView.Tree.AddChild(parent.FullPath, nodeDescription, modelIndex);
                modelWasReplaced = true;
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
                modelWasReplaced = false;
            }
        }

        /// <summary>Undo the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            if (modelWasReplaced)
            {
                Model parent = modelToInsert.Parent as Model;
                int modelIndex = parent.Children.IndexOf(modelToInsert as Model);
                try
                {
                    this.explorerView.Tree.Delete(this.modelToInsert.FullPath);
                    parent.Children.Remove(modelToInsert as Model);
                    parent.Children.Insert(modelIndex, modelToReplace as Model);
                    modelToReplace.Parent = parent;
                    var nodeDescription = presenter.GetNodeDescription(modelToReplace);
                    this.explorerView.Tree.AddChild(parent.FullPath, nodeDescription, modelIndex);
                }
                catch (Exception err)
                {
                    presenter.MainPresenter.ShowError(err);
                    modelWasReplaced = false;
                }
            }
        }
    }
}
