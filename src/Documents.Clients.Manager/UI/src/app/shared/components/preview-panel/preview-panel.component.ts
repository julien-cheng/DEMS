import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-preview-panel',
  templateUrl: './preview-panel.component.html',
  styleUrls: ['./preview-panel.component.scss']
})
export class PreviewPanelComponent implements OnInit {
  @Input() previewVisible: boolean;
  @Input() panelTabText: string;
  @Input() panelBottom: boolean = false;
  @Output() togglePreviewPanel = new EventEmitter();
  constructor() { }

  ngOnInit() {
  }

  closePreviewPanel() {
        this.previewVisible = !this.previewVisible;
        this.togglePreviewPanel.emit();
    }
}
