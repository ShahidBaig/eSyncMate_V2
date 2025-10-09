import { Component } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';

@Component({
    selector: 'page-footer',
    templateUrl: './page-footer.component.html',
    styleUrls: ['./page-footer.component.scss'],
    standalone: true,
    imports: [MatToolbarModule]
})
export class PageFooterComponent {

}
