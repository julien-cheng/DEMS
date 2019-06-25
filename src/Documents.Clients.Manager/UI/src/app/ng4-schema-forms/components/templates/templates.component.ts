import { Component, OnInit, Input, Output, EventEmitter, OnDestroy, ViewChild, ViewContainerRef, ComponentFactoryResolver, ComponentRef } from '@angular/core';
import { FormGroup, NgForm, AbstractControl } from '@angular/forms';
import { BaseControl } from '../../models/base-control.model';
@Component({
  selector: 'app-templates',
  template: `
      <div #container></div>
  `,
  styleUrls: ['./templates.component.scss']
})
export class TemplatesComponent implements OnInit, OnDestroy {
  @Input() mainFormControl: BaseControl[];
  @Input() formGroup: FormGroup;
  @Input() breakpoint: string;
  @Input() columns: string;
  @Output() updateFormValue = new EventEmitter();
  @ViewChild('container', { read: ViewContainerRef }) container: ViewContainerRef;
  private componentRef: ComponentRef<{}>;
  private mappings = {
    'column_1': Dynamic1ColumnComponent,
    'column_2': Dynamic2ColumnComponent,
    'column_3': Dynamic3ColumnComponent,
    'column_4': Dynamic4ColumnComponent
  };
  constructor(private componentFactoryResolver: ComponentFactoryResolver) { }

  ngOnInit() {
    if (this.columns) {
      let componentType = this.getComponentType('column_'+this.columns);

      // note: componentType must be declared within module.entryComponents
      let factory = this.componentFactoryResolver.resolveComponentFactory(componentType);
      this.componentRef = this.container.createComponent(factory);

      // set component context
      let instance = <DynamicComponent>this.componentRef.instance;
      instance.mainFormControl = this.mainFormControl;
      instance.formGroup = this.formGroup;
      instance.breakpoint =this.breakpoint;
      instance.updateFormValue.subscribe(($event) =>  this.updateFormValue.emit($event));
    }
  }
  ngOnDestroy() {
    if (this.componentRef) {
      this.componentRef.destroy();
      this.componentRef = null;
    }
  }
 
  //Maps the component to a specific column number
  getComponentType(typeName: string) {
    let type = this.mappings[typeName];
    return type || Dynamic1ColumnComponent;
  }
}
export abstract class DynamicComponent {
  mainFormControl: BaseControl[];
  formGroup: FormGroup;
  breakpoint: string;
  columnClass: string;
  @Output() updateFormValue = new EventEmitter();
}


@Component({
  selector: 'app-column-1',
  template: `
      <div *ngFor="let control of mainFormControl; let i = index;">
          <app-schema-form-control [form]="formGroup" [mainFormControl]="control" (updateFormValue)="updateFormValue.emit($event)"></app-schema-form-control>
      </div>
  `
})
export class Dynamic1ColumnComponent extends DynamicComponent { }


@Component({
  selector: 'app-column-2',
  template: `
    <div class="row align-items-center">
      <div *ngFor="let control of mainFormControl; let i = index;" [ngClass]="!!breakpoint? 'col-'+breakpoint+'-6' : 'col-6'">
          <app-schema-form-control [form]="formGroup" [mainFormControl]="control" (updateFormValue)="updateFormValue.emit($event)"></app-schema-form-control>
      </div>
    </div>
  `
})
export class Dynamic2ColumnComponent extends DynamicComponent { }


@Component({
  selector: 'app-column-3',
  template: `
    <div class="row">
      <div *ngFor="let control of mainFormControl; let i = index;"  [ngClass]="!!breakpoint? 'col-'+breakpoint+'-4' : 'col-4'">
          <app-schema-form-control [form]="formGroup" [mainFormControl]="control" (updateFormValue)="updateFormValue.emit($event)"></app-schema-form-control>
      </div>
    </div>
  `
})
export class Dynamic3ColumnComponent extends DynamicComponent { }

@Component({
  selector: 'app-column-4',
  template: `
    <div class="row">
      <div *ngFor="let control of mainFormControl; let i = index;" [ngClass]="!!breakpoint? 'col-'+breakpoint+'-3' : 'col-3'">
          <app-schema-form-control [form]="formGroup" [mainFormControl]="control" (updateFormValue)="updateFormValue.emit($event)"></app-schema-form-control>
      </div>
    </div>
  `
})
export class Dynamic4ColumnComponent extends DynamicComponent { }
