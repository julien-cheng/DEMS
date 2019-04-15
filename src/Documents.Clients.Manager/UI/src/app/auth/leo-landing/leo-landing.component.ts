import { Component, OnInit } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService, LoadingService, AppConfigService } from '../../shared/index';

@Component({
  selector: 'app-leo-landing',
  templateUrl: './leo-landing.component.html',
  styleUrls: ['./leo-landing.component.scss']
})
export class LeoLandingComponent implements OnInit {
  public authenticateUserForm: FormGroup;
  public authenticateInvalid = false;
  public isSaving = false;
  private token: string;
  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private router: Router,
    private formBuilder: FormBuilder,
    private loadingService: LoadingService,
    public appConfigService: AppConfigService
  ) {
    this.route.params.subscribe(params => {
      this.token = params.token;
    });
  }

  ngOnInit() {
    this.authenticateUserForm = this.formBuilder.group({
      email: '',
      password: ''
    });
    this.loadingService.setLoading(false);
  }

  authenticateRecipient(formValues) {
    this.isSaving = true;
    formValues.token = this.token;
    this.authService.leoAuthenticateUser(formValues).subscribe(
      resp => {
        if (resp.response.isAuthenticated) {
          this.appConfigService.setAPIConfiguration();
          !!resp.response.pathIdentifier
            ? this.router.navigate([
                '/manager/',
                resp.response.folderIdentifier.organizationKey,
                resp.response.folderIdentifier.folderKey,
                resp.response.pathIdentifier.pathKey
              ])
            : this.router.navigate(['/manager/', resp.response.folderIdentifier.organizationKey, resp.response.folderIdentifier.folderKey]);
        } else {
          this.authenticateInvalid = true;
          this.authenticateUserForm.reset();
        }
      },
      error => {
        console.error(error);
      },
      () => {
        this.isSaving = false;
      }
    );
  }

  cancel() {
    // Reset form
    this.authenticateUserForm.reset();
    this.authenticateInvalid = false;
  }
}
