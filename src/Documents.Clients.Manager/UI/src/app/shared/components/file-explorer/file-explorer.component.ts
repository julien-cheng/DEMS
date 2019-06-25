import { Component, OnInit, Input, Output, EventEmitter, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TreeModel, TreeNode, TREE_ACTIONS, KEYS, IActionMapping, TreeComponent } from 'angular-tree-component';
import { ExplorerService, IExplorer } from '../../services/explorer.service';
import { IBatchOperation, IRequestBatchData, ItemQueryType, IPathIdentifier, EventType} from '../../index';
import { BatchOperationService } from '../../services/batch-operation.service';

@Component({
    selector: 'file-explorer',
    templateUrl: './file-explorer.component.html',
    styleUrls: ['./file-explorer.component.scss']
})

export class FileExplorerComponent implements OnInit {
    @Input() isMoveAction: boolean = false; // Creates another node type without redirect actions
    @Input() currentTreeExplorer: any;
    @Input() activeNodeKey: string;
    @Output() saveFileMove = new EventEmitter();
    @Output() processBatchUiAction = new EventEmitter();
    @ViewChild(TreeComponent) private tree: TreeComponent;

    public fileExplorer: IExplorer;
    public identifier: IPathIdentifier;

    constructor(
        private batchOperationService: BatchOperationService,
        public explorerService: ExplorerService
    ) {
    }

    // File Explorer tree options
    fileExplorerTreeOptions = {
        allowDrag: false,
        allowDrop: false
    };

    ngOnInit() {
        this.fileExplorer = this.explorerService.fileExplorer;
        this.identifier = this.explorerService.fileExplorer.pathIdentifier;
        (!this.activeNodeKey) && this.explorerService.fileExplorer.activeNodeKey;
        (!this.currentTreeExplorer) &&
            (this.currentTreeExplorer = this.explorerService.fileExplorer.currentTreeExplorer);
    }

    onTreeInitialized() {
        // Sets the current active tree node based on active node key, and sets it to root if it's null
        // console.log('onTreeInitialized: ' + this.activeNodeKey);
        this.setCurrentTreeNodeActive();
    }

    onTreeUpdate() {
        //  console.log('onTreeUpdate: ' + this.activeNodeKey, this.isMoveAction);
        (this.activeNodeKey !== undefined) && this.setCurrentTreeNodeActive();
        (!this.isMoveAction && (!!this.currentTreeExplorer && this.currentTreeExplorer.length > 0)) &&
            this.setCurrentIsExpanded(this.currentTreeExplorer[0].children); //Do only for explorer
    }

    setCurrentTreeNodeActive() {
        const activeNodeKey = !!this.activeNodeKey ? this.activeNodeKey : 'root';
        if (activeNodeKey) {
            const activeTreeNode: TreeNode = this.tree.treeModel.getNodeById(activeNodeKey);
            activeTreeNode && activeTreeNode.setActiveAndVisible();
        }
    }

    // Top Level expand on data update (save changes) - Only expands (can be modified to collapse too)
    setCurrentIsExpanded(children: any) {
        const tree= this.tree;
        !!children && children.forEach(element => {
            let expanded = this.explorerService.getObjects(element, 'isExpanded', true);
            if (!!expanded && expanded.length > 0) {
                const expandedTreeNode: TreeNode = this.tree.treeModel.getNodeById(expanded[0].id);
                expandedTreeNode && expandedTreeNode.expand();
            }
        });
    }


    // Description: Returns the counts in each Main Directory
    childrenCount(node: TreeNode): string {
        return node && node.children ? `(${node.children.length})` : '';
    }

    // Description: Collapse File explorer
    collapseFileExplorer(collapsed: boolean) {
        this.explorerService.fileExplorer.isCollapsed = collapsed;
    }

    isDropTarget(itemQueryType: ItemQueryType): boolean {
        let t= this.batchOperationService.isPathMoveIntoItem(itemQueryType, 'targetPath');
        // console.log(itemQueryType, ' - SOURCE: ', t);
        return t;
    }

    // Modal Call
    saveMoveItem(event: Event, target: TreeNode) {
        event.preventDefault(); //Need to retrieve the source
        let rows: ItemQueryType[] = this.batchOperationService.batchRequest.rows,
            key = target.data.identifier;
        let batchoperations: IBatchOperation[] = [];
        batchoperations = <IBatchOperation[]>(this.batchOperationService.retrieveRequestOperation(rows, 'MoveIntoRequest'));
        
        batchoperations.filter((batchoperation) => {
            batchoperation.targetPathIdentifier = key;
            return true;
        });


        setTimeout(() => {
            target.treeModel.setFocusedNode(null);
            target.treeModel.activeNodeIds = {};
        });

        let requestBatchData: IRequestBatchData = { requestType: 'MoveIntoRequest', eventType: EventType.send, batchOperations: batchoperations };
       (batchoperations.length) && this.saveCallback(requestBatchData);
    }

    // Trigger the batch operation process
    saveCallback(requestBatchData: IRequestBatchData, rows?: ItemQueryType[]) {
        let data = !!rows ? Object.assign(requestBatchData, { rows: rows }) : requestBatchData;
        return this.processBatchUiAction.emit(data);
    }
}
