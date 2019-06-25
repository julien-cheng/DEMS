import { Directive } from '@angular/core';

@Directive({
  selector: '[appGotop]'
})
export class GotopDirective {

  constructor() {
    // Add Gotop arrow animation and clicks
    !$('#gotop').length && $('body').append('<a id="gotop"><i class="fa fa-angle-up"></i></a>').on('click', '#gotop', function () {
      $('html, body').animate({ scrollTop: 0 }, 'fast');
      $(this).removeClass('active');
      return false;
    });
    $('#gotop').fadeIn();

    $(window).scroll(function () {
      ($(window).scrollTop() <= 0) ? $('#gotop').removeClass('active') : $('#gotop').addClass('active');
    });
  }
}
