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
    let reviewsElement = document.createElement("div");
    reviewsElement.style.display = "flex";
    reviewsElement.style.flexDirection = "column";
    reviewsElement.style.rowGap = "2rem";

    reviews.forEach(review => {
        var root = document.createElement("div")
        root.innerHTML = reviewsHtml
            .replace("{AvatarUri}", review.Author.AvatarUri)
            .replace("{Username}", review.Author.Username)
            .replace("{Title}", review.Title)
            .replace("{Body}", review.Body);


        reviewsElement.appendChild(root);
    })
    
    return reviewsElement;
}

const reviewsHtml = `
<div style="display: grid; grid-template: 'avatar-section details-section' auto 'avatar-section footer-section' auto / 3.75rem; row-gap: 1rem; column-gap: 1.875rem;">
    <div>
      <img src="{AvatarUri}" alt="Avatar" width="62px" height="62px" style="border-radius: 50%">
    </div>
    <div style="display: flex; flex-direction: column;">
      <h5 style="display: inline-block; overflow-wrap: break-word; word-break: break-word; line-height: 1.5rem; 
      font-size: 1rem; font-weight: 600; font-family: Lato, Helvetica Neue, helvetica, sans-serif; margin: 0;">
      {Username}</h5>
      <p>Stars</p>
      <h3 style="font-family: Lato,Helvetica Neue,helvetica,sans-serif;line-height: 1.625rem;font-size: 1.25rem;font-weight: 600;">{Title}</p>
      <p style="font-size: 1rem;font-family: Lato, Helvetica Neue, helvetica, sans-serif;;font-weight: 500;line-height: 1.5rem;">{Body}</p>
    </div>
  <div></div>
  <div>
    <span>76 out of 85 users liked this</span>
  </div>
</div>
`