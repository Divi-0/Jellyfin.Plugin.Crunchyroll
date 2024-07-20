console.log("Hello from ExternalComments")



const observeUrlChange = () => {
    let oldHref = document.location.href;
    const body = document.querySelector("body");
    const observer = new MutationObserver(mutations => {
        if (oldHref !== document.location.href) {
            oldHref = document.location.href;
            featureSelect()
        }
    });
    observer.observe(body, { childList: true, subtree: true });
};

window.onload = observeUrlChange;

function featureSelect(){
    if(document.location.href.includes("details")){
        const urlParams = new URLSearchParams(document.location.hash.split("?")[1]);
        const id = urlParams.get('id');
        
        if(document.location.href.includes("context=tvshows")){
            showReviews(id);
        }
        
        showComments(id);
    }
    
}

async function showReviews(id) {
    const url = `${window.location.origin}/api/externalcomments/crunchyroll/reviews/${id}`
    let json;
    try {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`fetch reviews failed`);
        }

        json = await response.json();
    } catch (error) {
        console.error(error.message);
    }


    let reviewsElement = getReviewsHtml(json.Reviews);
    var elements = document.getElementsByClassName("detailPageContent");
    elements[0].appendChild(reviewsElement);
}

function showComments(id){
    
}

function getReviewsHtml(reviews){
    let reviewsWrapper = document.createElement("div");
    let reviewsElement = document.createElement("div");
    reviewsElement.id = "crunchyroll-reviews"
    reviewsElement.style.display = "flex";
    reviewsElement.style.flexDirection = "column";
    reviewsElement.style.rowGap = "2rem";
    reviewsElement.style.maxWidth = "54.375rem";

    reviewsWrapper.appendChild(reviewsElement);

    reviews.forEach(review => {
        var root = document.createElement("div")
        
        let starBody = "";
        for (let i = 0; i < review.AuthorRating; i++){
            starBody += fullStarSvg;
        }
        
        const maxRating = 5;
        for (let i = 0; i < maxRating - review.AuthorRating; i++){
            starBody += emptyStarSvg;
        }
        
        root.innerHTML = reviewsHtml
            .replace("{AvatarUri}", review.Author.AvatarUri)
            .replace("{Username}", review.Author.Username)
            .replace("{Title}", review.Title)
            .replace("{Body}", review.Body)
            .replace("{Stars}", starBody);


        reviewsElement.appendChild(root);
    })
    
    return reviewsWrapper;
}

const reviewsHtml = `
<div style="display: grid; grid-template: 'avatar-section details-section' auto 'avatar-section footer-section' auto / 3.75rem; row-gap: 1rem; column-gap: 1.875rem;">
    <div>
      <img src="{AvatarUri}" alt="Avatar" width="62px" height="62px" style="border-radius: 50%">
    </div>
    <div style="display: flex; flex-direction: column;">
      <h5 style="display: inline-block; overflow-wrap: break-word; word-break: break-word; line-height: 1.5rem; 
      font-size: 1rem; font-weight: 600; font-family: Lato, Helvetica Neue, helvetica, sans-serif; margin: 0 0 1rem 0;
       color: #fff;">
      {Username}</h5>
      <div style="display: flex; flex-direction: row; margin-bottom: 0.75rem; column-gap: 0.1rem">
          {Stars}
      </div>
      <div style="display: grid; grid-row-gap: 0.75rem">
          <h3 style="font-family: Lato,Helvetica Neue,helvetica,sans-serif;line-height: 1.625rem;font-size: 1.25rem;font-weight: 600; 
          margin: 0; color: #fff;">{Title}</h3>
          <p style="font-size: 1rem;font-family: Lato, Helvetica Neue, helvetica, sans-serif;;font-weight: 500;line-height: 1.5rem; 
          margin: 0;  color: #fff;">{Body}</p>
      </div>
    </div>
  <div></div>
  <div style="height: 24px">
    <span style="font-size: 0.875rem; color: #a0a0a0;">76 out of 85 users liked this</span>
  </div>
</div>
`

const fullStarSvg = `
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 576 512" style="width: 1.25rem"><!--!Font Awesome Free 6.6.0 by @fontawesome - https://fontawesome.com License - https://fontawesome.com/license/free Copyright 2024 Fonticons, Inc.--><path fill="#fff" d="M316.9 18C311.6 7 300.4 0 288.1 0s-23.4 7-28.8 18L195 150.3 51.4 171.5c-12 1.8-22 10.2-25.7 21.7s-.7 24.2 7.9 32.7L137.8 329 113.2 474.7c-2 12 3 24.2 12.9 31.3s23 8 33.8 2.3l128.3-68.5 128.3 68.5c10.8 5.7 23.9 4.9 33.8-2.3s14.9-19.3 12.9-31.3L438.5 329 542.7 225.9c8.6-8.5 11.7-21.2 7.9-32.7s-13.7-19.9-25.7-21.7L381.2 150.3 316.9 18z"/></svg>
`

const emptyStarSvg = `
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 576 512" style="width: 1.25rem"><!--!Font Awesome Free 6.6.0 by @fontawesome - https://fontawesome.com License - https://fontawesome.com/license/free Copyright 2024 Fonticons, Inc.--><path fill="#fff" d="M287.9 0c9.2 0 17.6 5.2 21.6 13.5l68.6 141.3 153.2 22.6c9 1.3 16.5 7.6 19.3 16.3s.5 18.1-5.9 24.5L433.6 328.4l26.2 155.6c1.5 9-2.2 18.1-9.7 23.5s-17.3 6-25.3 1.7l-137-73.2L151 509.1c-8.1 4.3-17.9 3.7-25.3-1.7s-11.2-14.5-9.7-23.5l26.2-155.6L31.1 218.2c-6.5-6.4-8.7-15.9-5.9-24.5s10.3-14.9 19.3-16.3l153.2-22.6L266.3 13.5C270.4 5.2 278.7 0 287.9 0zm0 79L235.4 187.2c-3.5 7.1-10.2 12.1-18.1 13.3L99 217.9 184.9 303c5.5 5.5 8.1 13.3 6.8 21L171.4 443.7l105.2-56.2c7.1-3.8 15.6-3.8 22.6 0l105.2 56.2L384.2 324.1c-1.3-7.7 1.2-15.5 6.8-21l85.9-85.1L358.6 200.5c-7.8-1.2-14.6-6.1-18.1-13.3L287.9 79z"/></svg>
`