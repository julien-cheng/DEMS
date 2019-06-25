import { Component, OnInit, Input } from '@angular/core';
import { IFile, IFileIdentifier, FileSetTypes, IMessaging } from '../../index';

@Component({
  selector: 'app-unknown',
  templateUrl: './unknown.component.html',
  styleUrls: ['./unknown.component.scss']
})
export class UnknownComponent implements OnInit {
  @Input() fileSet: FileSetTypes;
  @Input() viewerType: string;
  constructor() { }

  ngOnInit() {
  }

}
