import { Component, OnInit } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, LoadingService } from '../../shared/index';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  public loginForm: FormGroup;
  loginInvalid = false;

  constructor(
    private loadingService: LoadingService,
    private authService: AuthService,
    private router: Router,
    private formBuilder: FormBuilder
  ) {}

  ngOnInit() {
    this.loginForm = this.formBuilder.group({
      email: '',
      password: ''
    });
    this.loadingService.setLoading(false);
  }

  login(formValues) {
    this.authService.loginUser(formValues.userName, formValues.password).subscribe(resp => {
      if (!resp) {
        this.loginInvalid = true;
      } else {
        this.router.navigate(['/manager']);
      }
    });
  }

  cancel() {
    this.router.navigate(['/manager']);
  }
}
