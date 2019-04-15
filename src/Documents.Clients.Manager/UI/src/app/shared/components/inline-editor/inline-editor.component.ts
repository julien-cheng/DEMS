import {
  Component,
  Input,
  ElementRef,
  ViewChild,
  Renderer,
  forwardRef,
  OnInit,
  SimpleChanges,
  Output,
  EventEmitter,
  OnChanges
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormControl, Validators } from '@angular/forms';
import { ItemQueryType, IBatchOperation, ValidationPatterns, excludePatternValidator } from '../../index';

const INLINE_EDIT_CONTROL_VALUE_ACCESSOR = {
  provide: NG_VALUE_ACCESSOR,
  useExisting: forwardRef(() => InlineEditorComponent),
  multi: true
};
@Component({
  selector: 'app-inline-editor',
  templateUrl: './inline-editor.component.html',
  styleUrls: ['./inline-editor.component.scss']
})
export class InlineEditorComponent implements ControlValueAccessor, OnInit, OnChanges {
  @Output() saveChange = new EventEmitter();
  @Input() row: ItemQueryType;
  @Input() editing = true;
  @ViewChild('inlineEditControl') inlineEditControl: ElementRef; // input DOM element
  @Input() label: string; // Label value for input element
  @Input() type = 'text'; // The type of input element
  @Input() required = false; // Is input requried?
  @Input() inlineValidators: ValidationPatterns[];
  @Input() disabled = false; // Is input disabled?
  private _value = ''; // Private variable for input value
  private preValue = ''; // The value before clicking to edit
  public onChange: any = Function.prototype; // Trascend the onChange event
  public onTouched: any = Function.prototype; // Trascend the onTouch event
  public newPathName: FormControl;
  private patternRegex = '^[a-zA-Z0-9-_. ]+'; // This may come from the server
  public maxLength = 250; // This may come from the server
  constructor(element: ElementRef, private _renderer: Renderer) {}

  ngOnInit() {
    const validatorArr = [Validators.required, Validators.maxLength(this.maxLength)];

    if (!!this.inlineValidators && this.inlineValidators.length > 0) {
      this.inlineValidators.map((val: ValidationPatterns) => {
        val.isAllowed && !!val.pattern && validatorArr.push(Validators.pattern(val.pattern)); // = val.pattern
        !val.isAllowed && !!val.pattern && validatorArr.push(excludePatternValidator(val.pattern));
      });
    }
    this.newPathName = new FormControl('', Validators.compose(validatorArr));
  }

  ngOnChanges(simpleChanges: SimpleChanges) {
    !!simpleChanges.editing && simpleChanges.editing.currentValue && this.edit(this.label);
    simpleChanges.label && simpleChanges.label.currentValue && (this.value = this.label);
  }

  // Control Value Accessors for ngModel
  get value(): any {
    return this._value;
  }

  set value(v: any) {
    if (v !== this._value) {
      this._value = v;
      this.onChange(v);
    }
  }

  // Required for ControlValueAccessor interface
  writeValue(value: any) {
    if (value !== this._value) {
      this._value = value;
    }
  }

  // Required forControlValueAccessor interface
  public registerOnChange(fn: (_: any) => {}): void {
    this.onChange = fn;
  }

  // Required forControlValueAccessor interface
  public registerOnTouched(fn: () => {}): void {
    this.onTouched = fn;
  }

  // Do stuff when the input element loses focus
  saveInlineEdit($event: Event) {
    if (this.newPathName.invalid) {
      // Field is required and value is empty - handle empty editors
      this._value = this.preValue;
      return this.cancelInlineEdit($event);
    }

    this.row.allowedOperations.forEach(operation => {
      operation.batchOperation.type === 'RenameRequest' && (operation.batchOperation.newName = this._value);
    });

    this.saveChange.emit(this.row);
    this.editing = false;
  }

  // Description: cancel on blur or invalid entry
  cancelInlineEdit($event: Event) {
    const self = this;
    setTimeout(function() {
      // (needs a setTimeout) otherwise it interferes with saves button clicks
      self.editing = false;
      self.row.editMode = false;
    }, 500);
  }

  // Start the editing process for the input element
  edit(value) {
    if (this.disabled) {
      return;
    }

    this.preValue = value;
    this.editing = true;
    // Focus on the input element just as the editing begins
    setTimeout(() => this.inlineEditControl.nativeElement.focus());
  }
}
