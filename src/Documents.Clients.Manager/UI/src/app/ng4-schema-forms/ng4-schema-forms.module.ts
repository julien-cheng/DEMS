import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import {
  SchemaFormsComponent,
  SchemaFormControlComponent,
  SchemaForm,
  Validation,
  DescriptionComponent,
  MessagingComponent,
  StringControlComponent,
  BooleanControlComponent,
  ArrayControlComponent,
  NumberControlComponent,
  ObjectControlComponent,
  NullControlComponent,
  TemplatesComponent,
  Dynamic1ColumnComponent,
  Dynamic2ColumnComponent,
  Dynamic3ColumnComponent,
  Dynamic4ColumnComponent,
  FocusDirective
} from './index';

@NgModule({
  imports: [CommonModule, ReactiveFormsModule],
  declarations: [
    SchemaFormsComponent,
    SchemaFormControlComponent,
    StringControlComponent,
    BooleanControlComponent,
    ArrayControlComponent,
    NumberControlComponent,
    ObjectControlComponent,
    NullControlComponent,
    DescriptionComponent,
    MessagingComponent,
    TemplatesComponent,
    Dynamic1ColumnComponent,
    Dynamic2ColumnComponent,
    Dynamic3ColumnComponent,
    Dynamic4ColumnComponent,
    FocusDirective
  ],
  entryComponents: [Dynamic1ColumnComponent, Dynamic2ColumnComponent, Dynamic3ColumnComponent, Dynamic4ColumnComponent],
  exports: [SchemaFormsComponent],
  providers: [SchemaForm, Validation]
})
export class Ng4SchemaFormsModule {}
